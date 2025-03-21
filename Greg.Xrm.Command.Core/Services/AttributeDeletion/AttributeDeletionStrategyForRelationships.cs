using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	public class AttributeDeletionStrategyForRelationships(
		IOutput output
	)
		: AttributeDeletionStrategyBase
	{
		public override ComponentType ComponentType => ComponentType.Relationship;


		protected override async Task HandleInternalAsync(
			IOrganizationServiceAsync2 crm,
			AttributeMetadata attribute,
			IReadOnlyList<Dependency> dependencies)
		{
			var request = new RetrieveEntityRequest
			{
				EntityFilters = EntityFilters.Relationships,
				LogicalName = attribute.EntityLogicalName
			};

			output.Write($"Searching for relationships dependent by this column...");

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
			var tableMetadata = response.EntityMetadata;

			if (tableMetadata is null)
			{
				output.WriteLine("Done. No relationship found.", ConsoleColor.Green);
				return;
			}

			var oneToManyRelationships = tableMetadata.OneToManyRelationships
				.Where(r => r.ReferencedAttribute == attribute.LogicalName || r.ReferencingAttribute == attribute.LogicalName)
				.Select(r => r.SchemaName)
				.ToArray();
			var manyToManyRelationships = tableMetadata.ManyToManyRelationships
				.Where(r => r.Entity1IntersectAttribute == attribute.LogicalName || r.Entity2IntersectAttribute == attribute.LogicalName)
				.Select(r => r.SchemaName)
				.ToArray();

			var relationshipList = oneToManyRelationships.Union(manyToManyRelationships).ToList();

			output.WriteLine($"Done. Found {relationshipList.Count} relationships to delete.", ConsoleColor.Green);



			var i = 0;
			foreach (var relationship in relationshipList)
			{
				++i;
				output.Write($"Deleting relationship {i}/{relationshipList.Count} {relationship}...");
				await crm.ExecuteAsync(new DeleteRelationshipRequest
				{
					Name = relationship
				});
				output.WriteLine("Done", ConsoleColor.Green);
			}
		}
	}
}
