using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class Solution : EntityWrapper
	{
		protected Solution(Entity entity) : base(entity)
		{
		}


		public string uniquename => Get<string>();
		public string version => Get<string>();

		public bool ismanaged => Get<bool>();


		public string? PublisherCustomizationPrefix => GetAliased<string>("publisher", "customizationprefix");
		public string? PublisherUniqueName => GetAliased<string>("publisher", "uniquename");
		public int? PublisherOptionSetPrefix => GetAliased<int>("publisher", "customizationoptionvalueprefix");


		public class Repository : ISolutionRepository 
		{
			private readonly Dictionary<string, Solution> cache = new();

			public async Task<Solution?> GetByUniqueNameAsync(IOrganizationServiceAsync2 crm, string uniqueName)
			{
				if (cache.TryGetValue(uniqueName.ToLowerInvariant(), out var solution))
				{
					return solution;
				}


				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);
				var link = query.AddLink("publisher", "publisherid", "publisherid");
				link.Columns.AddColumns("customizationprefix", "uniquename", "customizationoptionvalueprefix");
				link.EntityAlias = "publisher";
				query.NoLock = true;
				query.TopCount = 1;

				var result = await crm.RetrieveMultipleAsync(query);

				solution = result.Entities.Select(x => new Solution(x)).FirstOrDefault();

				if (solution != null) cache[uniqueName.ToLowerInvariant()] = solution;

				return solution;
			}
		}
	}
}
