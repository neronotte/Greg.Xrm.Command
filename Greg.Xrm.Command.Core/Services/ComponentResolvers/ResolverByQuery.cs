using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Services.ComponentResolvers
{
	public class ResolverByQuery : IComponentResolver
	{
		private readonly IOrganizationServiceAsync2 crm;
		private readonly ILogger log;
		private readonly string table;
		private readonly string nameColumn;
		private readonly string? tableIdColumn;

		public ResolverByQuery(IOrganizationServiceAsync2 crm, ILogger log, string table, string nameColumn, string? tableIdColumn = null)
		{
			this.crm = crm;
			this.log = log;
			this.table = table;
			this.nameColumn = nameColumn;
			this.tableIdColumn = tableIdColumn ?? $"{table}id";
		}

		public async Task<Dictionary<Guid, string>> GetNamesAsync(IReadOnlyList<Guid> componentIdSet)
		{
			Dictionary<Guid, string> dict;
			try
			{
				var query = new QueryExpression(this.table);
				query.ColumnSet.AddColumns(this.nameColumn);
				query.Criteria.AddCondition(this.tableIdColumn, ConditionOperator.In, componentIdSet.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await this.crm.RetrieveMultipleAsync(query);

				dict = result.Entities.ToDictionary(x => x.Id, x => x.GetAttributeValue<string>(this.nameColumn));
			}
			catch(FaultException<OrganizationServiceFault>ex)
			{
				this.log.LogError(ex, "Error while retrieving {table} records: {message}", this.table, ex.Message);

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
	}
}