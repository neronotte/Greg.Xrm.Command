using Greg.Xrm.Command.Services;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public enum TeamType { Owner = 0, Access = 1, SecurityGroup = 2, Microsoft365Group = 3 }

	public class Team : EntityWrapper
	{
		private Team(Entity entity) : base(entity) { }
		public string name => this.Get<string>();
		public EntityReference? businessunitid => this.Get<EntityReference>();
		public OptionSetValue? teamtype => this.Get<OptionSetValue>();
		public string TypeName => teamtype == null ? "Unknown"
			: Enum.IsDefined(typeof(TeamType), teamtype.Value) ? ((TeamType)teamtype.Value).ToString() : teamtype.Value.ToString();
		public string BusinessUnitName => businessunitid?.Name ?? businessunitid?.Id.ToString() ?? string.Empty;

		public class Repository : ITeamRepository
		{
			public async Task<IReadOnlyList<Team>> SearchAsync(IOrganizationServiceAsync2 crm, TeamType? type, string? name, CancellationToken cancellationToken)
			{
				var query = CreateQuery();
				if (type.HasValue) query.Criteria.AddCondition("teamtype", ConditionOperator.Equal, (int)type.Value);
				if (!string.IsNullOrWhiteSpace(name)) query.Criteria.AddCondition("name", ConditionOperator.Like, LikeExpression.Contains(name.Trim()));
				return await crm.RetrieveAllAsync(query, entity => new Team(entity), cancellationToken);
			}

			public async Task<IReadOnlyList<Team>> GetByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken)
			{
				var query = CreateQuery();
				query.Distinct = true;
				query.AddLink("teammembership", "teamid", "teamid", JoinOperator.Inner)
					.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);
				return await crm.RetrieveAllAsync(query, entity => new Team(entity), cancellationToken);
			}

			private static QueryExpression CreateQuery() => new("team")
			{
				ColumnSet = new ColumnSet("teamid", "name", "businessunitid", "teamtype"),
				Orders = { new OrderExpression("name", OrderType.Ascending), new OrderExpression("teamid", OrderType.Ascending) }
			};

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