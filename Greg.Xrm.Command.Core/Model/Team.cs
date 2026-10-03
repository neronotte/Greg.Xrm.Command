using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class Team : EntityWrapper
	{
		private Team(Entity entity) : base(entity) { }
		public string name => this.Get<string>();
		public EntityReference? businessunitid => this.Get<EntityReference>();
		public OptionSetValue? teamtype => this.Get<OptionSetValue>();

		public class Repository
		{
			public async Task<Team> ResolveAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("team") { ColumnSet = new ColumnSet("name", "businessunitid", "teamtype"), TopCount = 2 };
				if (Guid.TryParse(identifier, out var teamId))
					query.Criteria.AddCondition("teamid", ConditionOperator.Equal, teamId);
				else
					query.Criteria.AddCondition("name", ConditionOperator.Equal, identifier);
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (response.Entities.Count == 0)
					throw new InvalidOperationException($"Team '{identifier}' was not found.");
				if (response.Entities.Count > 1)
					throw new InvalidOperationException($"Multiple teams named '{identifier}' were found. Specify the GUID with --team.");
				return new Team(response.Entities[0]);
			}
		}
	}
}