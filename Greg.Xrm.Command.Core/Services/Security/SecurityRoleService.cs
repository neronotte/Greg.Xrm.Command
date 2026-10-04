using Greg.Xrm.Command.Services;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Security
{
	public sealed record SecurityRoleInfo(Guid RoleId, string Name, string BusinessUnit, bool IsManaged)
	{
		public Guid? BusinessUnitId { get; init; }
	}
	public sealed record SecurityRoleAssignmentInfo(SecurityRoleInfo Role, IReadOnlyCollection<string> Sources);

	public sealed class SecurityRoleService : ISecurityRoleService
	{
		public async Task<IReadOnlyList<SecurityRoleInfo>> GetRolesByIdentifierAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken)
		{
			var query = CreateRoleQuery();
			query.TopCount = 2;
			if (Guid.TryParse(identifier, out var roleId))
			{
				query.Criteria.AddCondition("roleid", ConditionOperator.Equal, roleId);
			}
			else
			{
				query.Criteria.AddCondition("parentroleid", ConditionOperator.Null);
				query.Criteria.AddCondition("name", ConditionOperator.Equal, identifier);
			}

			var response = await crm.RetrieveMultipleAsync(query, cancellationToken);
			return response.Entities.Select(MapRole).ToList();
		}

		/// <summary>
		/// Returns the root security roles (one per role definition). Dataverse creates a copy of each role
		/// for every business unit: copies are excluded by filtering on parentroleid.
		/// </summary>
		public async Task<IReadOnlyList<SecurityRoleInfo>> GetRolesAsync(IOrganizationServiceAsync2 crm, bool unmanagedOnly, CancellationToken cancellationToken)
		{
			return await GetRolesAsync(crm, unmanagedOnly, null, cancellationToken);
		}

		/// <summary>
		/// Returns the root security roles, optionally filtered to the ones whose name contains <paramref name="nameFilter"/>.
		/// </summary>
		public async Task<IReadOnlyList<SecurityRoleInfo>> GetRolesAsync(IOrganizationServiceAsync2 crm, bool unmanagedOnly, string? nameFilter, CancellationToken cancellationToken)
		{
			var query = CreateRoleQuery();
			query.Criteria.AddCondition("parentroleid", ConditionOperator.Null);
			if (unmanagedOnly)
			{
				query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
			}
			if (!string.IsNullOrWhiteSpace(nameFilter))
			{
				query.Criteria.AddCondition("name", ConditionOperator.Like, LikeExpression.Contains(nameFilter));
			}

			var roles = await crm.RetrieveAllAsync(query, MapRole, cancellationToken);
			return roles.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}

		public async Task<IReadOnlyList<SecurityRoleAssignmentInfo>> GetRolesByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken)
		{
			var assignments = new Dictionary<Guid, (SecurityRoleInfo Role, HashSet<string> Sources)>();

			foreach (var role in await GetDirectRolesByUserAsync(crm, userId, cancellationToken))
			{
				AddRole(assignments, role, "Direct");
			}

			foreach (var role in await GetTeamRolesByUserAsync(crm, userId, cancellationToken))
			{
				AddRole(assignments, role, "Team");
			}

			return assignments.Values
				.Select(x => new SecurityRoleAssignmentInfo(x.Role, x.Sources.OrderBy(s => s).ToArray()))
				.OrderBy(x => x.Role.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static QueryExpression CreateRoleQuery()
		{
			return new QueryExpression("role")
			{
				NoLock = true,
				ColumnSet = new ColumnSet("roleid", "name", "businessunitid", "ismanaged"),
				Distinct = true,
				Orders = { new OrderExpression("roleid", OrderType.Ascending) }
			};
		}

		private static SecurityRoleInfo MapRole(Entity role)
		{
			return new SecurityRoleInfo(
				role.Id,
				role.GetAttributeValue<string>("name") ?? string.Empty,
				role.GetAttributeValue<EntityReference>("businessunitid")?.Name
					?? role.GetAttributeValue<EntityReference>("businessunitid")?.Id.ToString()
					?? string.Empty,
				role.GetAttributeValue<bool?>("ismanaged") ?? false)
			{
				BusinessUnitId = role.GetAttributeValue<EntityReference>("businessunitid")?.Id
			};
		}

		private static void AddRole(Dictionary<Guid, (SecurityRoleInfo Role, HashSet<string> Sources)> assignments, SecurityRoleInfo role, string source)
		{
			if (!assignments.TryGetValue(role.RoleId, out var entry))
			{
				entry = (role, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			}

			entry.Sources.Add(source);
			assignments[role.RoleId] = entry;
		}

		public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<SecurityRoleInfo>>> GetRolesByTeamsAsync(IOrganizationServiceAsync2 crm, IEnumerable<Guid> teamIds, CancellationToken cancellationToken)
		{
			var ids = teamIds.Where(id => id != Guid.Empty).Distinct().ToArray();
			if (ids.Length == 0) return new Dictionary<Guid, IReadOnlyList<SecurityRoleInfo>>();
			var query = CreateRoleQuery();
			var link = query.AddLink("teamroles", "roleid", "roleid", JoinOperator.Inner);
			link.EntityAlias = "assignment";
			link.Columns = new ColumnSet("teamid");
			link.LinkCriteria.AddCondition("teamid", ConditionOperator.In, ids.Cast<object>().ToArray());
			query.Orders.Add(new OrderExpression("teamid", OrderType.Ascending) { EntityName = "assignment" });
			var rows = await crm.RetrieveAllAsync(query, entity =>
				(TeamId: (Guid)entity.GetAttributeValue<AliasedValue>("assignment.teamid").Value, Role: MapRole(entity)), cancellationToken);
			return rows.GroupBy(row => row.TeamId).ToDictionary(group => group.Key,
				group => (IReadOnlyList<SecurityRoleInfo>)group.Select(row => row.Role).DistinctBy(role => role.RoleId)
					.OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase).ThenBy(role => role.RoleId).ToArray());
		}

		public async Task<IReadOnlyList<SecurityRoleInfo>> GetDirectRolesByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken)
		{
			var query = CreateRoleQuery();
			var link = query.AddLink("systemuserroles", "roleid", "roleid", JoinOperator.Inner);
			link.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);

			return await crm.RetrieveAllAsync(query, MapRole, cancellationToken);
		}

		private static async Task<IReadOnlyList<SecurityRoleInfo>> GetTeamRolesByUserAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken)
		{
			var query = CreateRoleQuery();
			var teamRoles = query.AddLink("teamroles", "roleid", "roleid", JoinOperator.Inner);
			var membership = teamRoles.AddLink("teammembership", "teamid", "teamid", JoinOperator.Inner);
			membership.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);

			return await crm.RetrieveAllAsync(query, MapRole, cancellationToken);
		}
	}
}
