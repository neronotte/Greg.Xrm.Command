using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class SecurityRole : EntityWrapper
	{
		private SecurityRole(Entity entity) : base(entity) { }
		public string name => this.Get<string>();
		public EntityReference? businessunitid => this.Get<EntityReference>();

		public class Repository
		{
			public async Task<SecurityRole> ResolveAsync(IOrganizationServiceAsync2 crm, string identifier, Guid businessUnitId, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("role") { ColumnSet = new ColumnSet("name", "businessunitid"), TopCount = 2 };
				query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitId);
				if (Guid.TryParse(identifier, out var roleId))
					query.Criteria.AddCondition("roleid", ConditionOperator.Equal, roleId);
				else
					query.Criteria.AddCondition("name", ConditionOperator.Equal, identifier);
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (response.Entities.Count == 0)
					throw new InvalidOperationException($"Security role '{identifier}' was not found in business unit '{businessUnitId}'. When using a GUID, select the role copy in that business unit.");
				if (response.Entities.Count > 1)
					throw new InvalidOperationException($"Multiple security roles named '{identifier}' were found in business unit '{businessUnitId}'. Specify the GUID with --role.");
				return new SecurityRole(response.Entities[0]);
			}

			public async Task<bool> IsAssignedAsync(IOrganizationServiceAsync2 crm, Guid roleId, EntityReference recipient, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("role") { ColumnSet = new ColumnSet(false), TopCount = 1 };
				query.Criteria.AddCondition("roleid", ConditionOperator.Equal, roleId);
				var user = recipient.LogicalName == "systemuser";
				var link = query.AddLink(user ? "systemuserroles" : "teamroles", "roleid", "roleid", JoinOperator.Inner);
				link.LinkCriteria.AddCondition(user ? "systemuserid" : "teamid", ConditionOperator.Equal, recipient.Id);
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return response.Entities.Count > 0;
			}
		}
	}
}