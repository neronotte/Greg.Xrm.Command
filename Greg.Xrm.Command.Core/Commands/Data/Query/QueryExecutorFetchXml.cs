using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public class QueryExecutorFetchXml(string fetchXml) : IQueryExecutor
	{
		public async Task<IReadOnlyCollection<Entity>> ExecuteQueryAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
		{
			var query = new FetchExpression(fetchXml);
			var result = await crm.RetrieveAllAsync(query, cancellationToken);
			return result.Entities;
		}
	}
}
