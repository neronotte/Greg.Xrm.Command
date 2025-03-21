using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	public class AttributeDeletionStrategyForViews(
		IOutput output,
		ISavedQueryRepository savedQueryRepository,
		IUserQueryRepository userQueryRepository
	)
		: AttributeDeletionStrategyBase
	{
		public override ComponentType ComponentType => ComponentType.SavedQuery;


		protected override async Task HandleInternalAsync(
			IOrganizationServiceAsync2 crm,
			AttributeMetadata attribute,
			IReadOnlyList<Dependency> dependencies)
		{
			var viewList = new List<TableView>();

			var savedQueries = await savedQueryRepository.GetByIdAsync(crm, dependencies.Select(x => x.dependentcomponentobjectid));
			viewList.AddRange(savedQueries);

			var userQueries = await userQueryRepository.GetContainingAsync(crm, attribute.EntityLogicalName, attribute.SchemaName);
			viewList.AddRange(userQueries);

			var i = 0;
			foreach (var e in viewList)
			{
				++i;

				output.Write($"Updating {e.EntityName} {i}/{viewList.Count} {e.name}...");

				e.fetchxml = RemoveFieldFromFetchXml(e.fetchxml, attribute.LogicalName);

				if (e.layoutxml is not null)
				{
					e.layoutxml = RemoveFieldFromFetchXml(e.layoutxml, attribute.LogicalName);
				}

				await e.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);
			}
		}
	}
}
