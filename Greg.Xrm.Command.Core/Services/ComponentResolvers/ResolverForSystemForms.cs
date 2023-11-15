using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Services.ComponentResolvers
{
	public class ResolverForSystemForms : IComponentResolver
	{
		private readonly IOrganizationServiceAsync2 crm;
		private readonly ILogger log;

		public ResolverForSystemForms(IOrganizationServiceAsync2 crm, ILogger log)
		{
			this.crm = crm;
			this.log = log;
		}


		public async Task<Dictionary<Guid, string>> GetNamesAsync(IReadOnlyList<Guid> componentIdSet)
		{
			Dictionary<Guid, string> dict;
			try
			{
				var query = new QueryExpression("systemform");
				query.ColumnSet.AddColumns("objecttypecode", "type", "name", "description");
				query.Criteria.AddCondition("formid", ConditionOperator.In, componentIdSet.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await this.crm.RetrieveMultipleAsync(query);

				dict = result.Entities.ToDictionary(x => x.Id, x => GetLabel(x));
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				this.log.LogError(ex, "Error while retrieving systemform records: {message}", ex.Message);
				dict = new();
			}

			foreach (var id in componentIdSet)
			{
				if (!dict.ContainsKey(id))
				{
					dict.Add(id, $"[Missing: {id}]");
				}
			}
			return dict;
		}

		private string GetLabel(Entity entity)
		{
			var objectTypeCode = entity.GetFormattedValue("objecttypecode");
			var type = entity.GetFormattedValue("type");
			var name = entity.GetAttributeValue<string>("name");

			return $"{objectTypeCode}, {type} form: {name}";
		}
	}
}
