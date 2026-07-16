using System.Xml.Linq;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public class QueryExecutorFetchXml(string fetchXml) : IQueryExecutor
	{
		public async Task<IReadOnlyCollection<Entity>> ExecuteQueryAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
		{
			var fetchDoc = XDocument.Parse(fetchXml);
			var fetchElement = fetchDoc.Root!;

			var entities = new List<Entity>();
			int page = 1;
			string? pagingCookie = null;

			EntityCollection result;
			do
			{
				fetchElement.SetAttributeValue("page", page);
				if (pagingCookie != null)
					fetchElement.SetAttributeValue("paging-cookie", pagingCookie);

				var query = new FetchExpression(fetchDoc.ToString());
				result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				entities.AddRange(result.Entities);

				if (result.MoreRecords)
				{
					page++;
					pagingCookie = result.PagingCookie;
				}
			}
			while (result.MoreRecords);

			return entities;
		}
	}
}
