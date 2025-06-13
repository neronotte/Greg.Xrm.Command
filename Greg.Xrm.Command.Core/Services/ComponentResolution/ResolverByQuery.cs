using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Services.ComponentResolution
{
	public class ResolverByQuery : IComponentResolver
	{
		private readonly ILogger log;
		private readonly string table;
		private string nameColumn;
		private string tableIdColumn;

		public ResolverByQuery(ILogger log, string table, string nameColumn, string tableIdColumn = null)
		{
			this.log = log;
			this.table = table;
			this.nameColumn = nameColumn;
			this.tableIdColumn = tableIdColumn;
		}

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
			if (nameColumn == null || tableIdColumn == null)
			{
				var request = new RetrieveEntityRequest
				{
					LogicalName = table,
					EntityFilters = EntityFilters.Entity
				};

				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);

				var metadata = response.EntityMetadata;

				if (nameColumn == null)
				{
					nameColumn = metadata.PrimaryNameAttribute;
				}
				if (tableIdColumn == null)
				{
					tableIdColumn = metadata.PrimaryIdAttribute;
				}
			}	



			Dictionary<Guid, string> dict;
			try
			{
				var query = new QueryExpression(table);
				query.ColumnSet.AddColumns(nameColumn);
				query.Criteria.AddCondition(tableIdColumn, ConditionOperator.In, componentIdSet.Cast<object>().ToArray());
				query.NoLock = true;

				var result = await crm.RetrieveMultipleAsync(query);

				dict = result.Entities.ToDictionary(x => x.Id, x => x.GetAttributeValue<string>(nameColumn));
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				log.LogError(ex, "Error while retrieving {Table} records: {Message}", table, ex.Message);
				dict = new Dictionary<Guid, string>();
			}
			return dict;
		}
	}
}
