using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class BusinessUnit : EntityWrapper
	{
		private BusinessUnit(Entity entity) : base(entity) { }
		public string name => this.Get<string>();

		public class Repository
		{
			public async Task<BusinessUnit> ResolveAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("businessunit") { ColumnSet = new ColumnSet("name"), TopCount = 2 };
				if (Guid.TryParse(identifier, out var businessUnitId))
					query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitId);
				else
					query.Criteria.AddCondition("name", ConditionOperator.Equal, identifier);
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (response.Entities.Count == 0)
					throw new InvalidOperationException($"Business unit '{identifier}' was not found.");
				if (response.Entities.Count > 1)
					throw new InvalidOperationException($"Multiple business units named '{identifier}' were found. Specify the GUID with --businessunit.");
				return new BusinessUnit(response.Entities[0]);
			}
		}
	}
}