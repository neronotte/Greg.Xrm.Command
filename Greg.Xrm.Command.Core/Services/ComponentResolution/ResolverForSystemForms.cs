using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.ComponentResolution
{
	public class ResolverForSystemForms(ILogger log) : IComponentResolver
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


		public async Task<Dictionary<Guid, string>> GetNamesAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<Guid> componentIdSet)
		{
			Dictionary<Guid, string> dict;
			try
			{
				var query = new QueryExpression("systemform");
				query.ColumnSet.AddColumns("objecttypecode", "type", "name", "description");
				query.Criteria.AddCondition("formid", ConditionOperator.In, componentIdSet.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				dict = result.Entities.ToDictionary(x => x.Id, x => GetLabel(x));
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				log.LogError(ex, "Error while retrieving systemform records: {Message}", ex.Message);
				dict = [];
			}

			return dict;
		}

		private static string GetLabel(Entity entity)
		{
			var objectTypeCode = entity.GetFormattedValue("objecttypecode");
			var type = entity.GetFormattedValue("type");
			var name = entity.GetAttributeValue<string>("name");

			return $"{objectTypeCode}, {type} form: {name}";
		}
	}
}
