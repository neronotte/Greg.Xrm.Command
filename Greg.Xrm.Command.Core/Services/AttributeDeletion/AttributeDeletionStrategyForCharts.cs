using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	public class AttributeDeletionStrategyForCharts(IOutput output) 
		: AttributeDeletionStrategyBase
	{
		public override ComponentType ComponentType => ComponentType.SavedQueryVisualization;


		protected override async Task HandleInternalAsync(
			IOrganizationServiceAsync2 crm, 
			AttributeMetadata attribute, 
			IReadOnlyList<Dependency> dependencies)
		{
			var result = await RetrieveDataAsync(crm, dependencies, "savedqueryvisualization", "savedqueryvisualizationid", "name", "datadescription");

			var i = 0;
			foreach (var e in result)
			{
				++i;

				output.Write($"Updating savedqueryvisualization {i}/{result.Count} {e.GetAttributeValue<string>("name")}...");
				e["datadescription"] = RemoveFieldFromFetchXml(e.GetAttributeValue<string>("datadescription"), attribute.LogicalName);
				await crm.UpdateAsync(e);
				output.WriteLine("Done", ConsoleColor.Green);
			}
		}
	}
}
