using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
	/// <summary>
	/// Mappings are not identified as dependencies, so we need to query them separately
	/// </summary>
	/// <param name="output"></param>
	public class AttributeDeletionStrategyForMappings(IOutput output) : IAttributeDeletionStrategy
	{

		public async Task HandleAsync(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, DependencyList dependencies)
		{
			var query = new QueryExpression("attributemap");
			query.ColumnSet.AddColumns("sourceattributename", "targetattributename");
			query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
			query.Criteria.AddCondition("issystem", ConditionOperator.Equal, false);

			var filter = query.Criteria.AddFilter(LogicalOperator.Or);
			filter.AddCondition("sourceattributename", ConditionOperator.Equal, attribute.LogicalName);
			filter.AddCondition("targetattributename", ConditionOperator.Equal, attribute.LogicalName);

			var entityLink = query.AddLink("entitymap", "entitymapid", "entitymapid");
			entityLink.Columns.AddColumns("sourceentityname", "targetentityname");
			entityLink.LinkCriteria.FilterOperator = LogicalOperator.Or;
			entityLink.LinkCriteria.AddCondition("sourceentityname", ConditionOperator.Equal, attribute.EntityLogicalName);
			entityLink.LinkCriteria.AddCondition("targetentityname", ConditionOperator.Equal, attribute.EntityLogicalName);
			entityLink.EntityAlias = "e";

			query.NoLock = true;

			var result = await crm.RetrieveMultipleAsync(query);

			var attributeKey = attribute.EntityLogicalName + "." + attribute.LogicalName;

			var maps = result.Entities.Select(x =>
			new
			{
				x.Id,
				x.LogicalName,
				Source = x.GetAliasedValue<string>("e.sourceentityname") + "." + x.GetAttributeValue<string>("sourceattributename"),
				Target = x.GetAliasedValue<string>("e.targetentityname") + "." + x.GetAttributeValue<string>("targetattributename"),
			})
			.Where(x => string.Equals(attributeKey, x.Source, StringComparison.OrdinalIgnoreCase) || string.Equals(attributeKey, x.Target, StringComparison.OrdinalIgnoreCase))
			.ToArray();



			var i = 0;
			foreach (var e in maps)
			{
				++i;
				output.Write($"Deleting attributemap {i}/{result.Entities.Count} {e.Id}...");
				await crm.DeleteAsync(e.LogicalName, e.Id);
				output.WriteLine("Done", ConsoleColor.Green);
			}
		}
	}
}
