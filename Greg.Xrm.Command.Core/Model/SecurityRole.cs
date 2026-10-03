using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public class SecurityRole : EntityWrapper
	{
		private SecurityRole(Entity entity) : base(entity) { }
		public string name => this.Get<string>();
		public EntityReference? businessunitid => this.Get<EntityReference>();
		public string? description => this.Get<string>();
		public OptionSetValue? isinherited => this.Get<OptionSetValue>();
		public bool? ismanaged => this.Get<bool?>();

		public class Repository
		{
			public async Task<SecurityRole> GetByIdentifierAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("role")
				{
					ColumnSet = new ColumnSet("name", "businessunitid", "description", "isinherited", "ismanaged"), TopCount = 2
				};
				if (Guid.TryParse(identifier, out var roleId))
					query.Criteria.AddCondition("roleid", ConditionOperator.Equal, roleId);
				else
				{
					query.Criteria.AddCondition("parentroleid", ConditionOperator.Null);
					query.Criteria.AddCondition("name", ConditionOperator.Equal, identifier);
				}
				var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (response.Entities.Count == 0)
					throw new InvalidOperationException($"Security role '{identifier}' was not found.");
				if (response.Entities.Count > 1)
					throw new InvalidOperationException($"Multiple root security roles named '{identifier}' were found. Specify the GUID with --role.");
				return new SecurityRole(response.Entities[0]);
			}

			public Task DeleteAsync(IOrganizationServiceAsync2 crm, Guid roleId, CancellationToken cancellationToken)
			{
				return crm.DeleteAsync("role", roleId, cancellationToken);
			}

			public async Task<IReadOnlyList<string>> GetNamesAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
			{
				var query = new QueryExpression("role")
				{
					ColumnSet = new ColumnSet("name"), Orders = { new OrderExpression("roleid", OrderType.Ascending) }
				};
				return await crm.RetrieveAllAsync(query, entity => entity.GetAttributeValue<string>("name") ?? string.Empty, cancellationToken);
			}

			public async Task<(Guid RoleId, int PrivilegeCount)> CloneAsync(IOrganizationServiceAsync2 crm, Guid sourceId,
				string name, string? description, Guid businessUnitId, int inheritance, CancellationToken cancellationToken)
			{
				var response = (RetrieveRolePrivilegesRoleResponse)await crm.ExecuteAsync(
					new RetrieveRolePrivilegesRoleRequest { RoleId = sourceId }, cancellationToken);
				var privileges = response.RolePrivileges.Select(privilege => new RolePrivilege
				{
					PrivilegeId = privilege.PrivilegeId, Depth = privilege.Depth
				}).ToArray();
				var roleId = Guid.NewGuid();
				var role = new Entity("role", roleId)
				{
					["name"] = name,
					["description"] = description,
					["businessunitid"] = new EntityReference("businessunit", businessUnitId),
					["isinherited"] = new OptionSetValue(inheritance)
				};
				await crm.ExecuteAsync(new ExecuteTransactionRequest
				{
					Requests =
					[
						new CreateRequest { Target = role },
						new ReplacePrivilegesRoleRequest { RoleId = roleId, Privileges = privileges }
					],
					ReturnResponses = false
				}, cancellationToken);
				return (roleId, privileges.Length);
			}

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