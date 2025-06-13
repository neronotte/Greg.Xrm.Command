using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.ComponentResolution
{
	public class ResolverForWorkflows(ILogger log) : IComponentResolver
	{

		public async Task ResolveAsync(IReadOnlyCollection<SolutionComponent> componentList, IOrganizationServiceAsync2 crm)
		{
			if (componentList.Count == 0) return;
			var idList = componentList
				.Select(_ => _.objectid)
				.ToList();

			var dict = await GetNamesAsync(crm, idList);
			foreach (var id in idList)
			{
				if (!dict.TryGetValue(id, out var name))
				{
					name = $"[Missing: {id}]";
				}
				var leaf = componentList.First(_ => _.objectid == id);
				leaf.Label = name;
			}
		}




		private async Task<Dictionary<Guid, string>> GetNamesAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<Guid> componentIdSet)
		{

			Dictionary<Guid, string> dict;
			try
			{
				var query = new QueryExpression("workflow");
				query.ColumnSet.AddColumns("uniquename", "name");
				query.Criteria.AddCondition("workflowid", ConditionOperator.In, componentIdSet.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				dict = result.Entities.ToDictionary(x => x.Id, GetLabelFromWorkflow);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				log.LogError(ex, "Error while retrieving workflow records: {Message}", ex.Message);
				dict = new Dictionary<Guid, string>();
			}
			return dict;
		}

		private static string GetLabelFromWorkflow(Entity entity)
		{
			return $"{entity.GetAttributeValue<string>("name")} ({entity.GetAttributeValue<string>("uniquename")})"
				.Replace("()", string.Empty)
				.Trim();
		}
	}
}
