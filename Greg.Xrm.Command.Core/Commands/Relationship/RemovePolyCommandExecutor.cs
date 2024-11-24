using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Relationship
{
	public class RemovePolyCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) 
		: ICommandExecutor<RemovePolyCommand>
	{
		private readonly IOutput output = output;
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository;

		public async Task<CommandResult> ExecuteAsync(RemovePolyCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);



			this.output.Write($"Retrieving metadata for entity {command.ChildTable}...");
			EntityMetadata entityMetadata;
			try
			{
				var request = new RetrieveEntityRequest();
				request.LogicalName = command.ChildTable;
				request.EntityFilters = EntityFilters.Attributes | EntityFilters.Relationships;

				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);

				entityMetadata = response.EntityMetadata;
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}



			var lookupColumn = entityMetadata.Attributes
				.OfType<LookupAttributeMetadata>()
				.FirstOrDefault(x => x.LogicalName == command.LookupColumnName);
			if (lookupColumn == null)
			{
				return CommandResult.Fail($"There's no lookup column with the name provided ({command.LookupColumnName}).");
			}


			var relationshipList = Array.FindAll(entityMetadata.ManyToOneRelationships, x => x.ReferencingAttribute == command.LookupColumnName);
			if (relationshipList == null || relationshipList.Length == 0)
			{
				return CommandResult.Fail($"There's no relationship pointed by the lookup column provided ({command.LookupColumnName}).");
			}

			var existingRelationship = Array.Find(relationshipList, x => x.ReferencedEntity == command.ParentTable);
			if (existingRelationship == null)
			{
				return CommandResult.Fail($"The relationship between {command.ChildTable} and {command.ParentTable} via {command.LookupColumnName} does not exists!");
			}


			this.output.Write($"Deleting relationship {existingRelationship.SchemaName}...");
			try
			{
				var request = new DeleteRelationshipRequest
				{
					Name = existingRelationship.SchemaName
				};
				await crm.ExecuteAsync(request);

				this.output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch(Exception ex)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
