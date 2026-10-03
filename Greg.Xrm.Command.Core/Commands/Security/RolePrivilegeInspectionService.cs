using Greg.Xrm.Command.Commands.Security.Roles;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Security
{
	public sealed record RolePrivilegeCell(PrivilegeType Type, Guid? PrivilegeId, string? Name, bool IsAssigned, int? Level, bool MatchesFilter);
	public sealed record RoleTablePrivileges(string LogicalName, string DisplayName, IReadOnlyList<RolePrivilegeCell> Cells);
	public sealed record RoleMiscellaneousPrivilege(Guid PrivilegeId, string Name, bool IsAssigned, int? Level);
	public sealed record RolePrivilegeSnapshot(IReadOnlyList<RoleTablePrivileges> Tables, IReadOnlyList<RoleMiscellaneousPrivilege> Miscellaneous, IReadOnlyList<string> Warnings);

	public class RolePrivilegeInspectionService(Privilege.Repository privilegeRepository)
	{
		public static IReadOnlyList<PrivilegeType> Actions { get; } = Array.AsReadOnly(new[]
		{
			PrivilegeType.Create, PrivilegeType.Read, PrivilegeType.Write, PrivilegeType.Delete,
			PrivilegeType.Append, PrivilegeType.AppendTo, PrivilegeType.Assign, PrivilegeType.Share
		});

		public async Task<RolePrivilegeSnapshot> InspectAsync(IOrganizationServiceAsync2 crm, Guid roleId, RolePrivilegeViewMode mode, string? tableFilter, string? privilegeFilter, CancellationToken cancellationToken)
		{
			var assignedResponse = (RetrieveRolePrivilegesRoleResponse)await crm.ExecuteAsync(new RetrieveRolePrivilegesRoleRequest { RoleId = roleId }, cancellationToken);
			var assigned = (assignedResponse.RolePrivileges ?? [])
				.GroupBy(privilege => privilege.PrivilegeId)
				.ToDictionary(group => group.Key, group => group.OrderByDescending(privilege => privilege.Depth).First());
			var metadataResponse = (RetrieveAllEntitiesResponse)await crm.ExecuteAsync(new RetrieveAllEntitiesRequest
			{
				EntityFilters = EntityFilters.Entity | EntityFilters.Privileges,
				RetrieveAsIfPublished = false
			}, cancellationToken);
			var catalog = await privilegeRepository.GetAllAsync(crm, cancellationToken);
			var catalogById = catalog.ToDictionary(privilege => privilege.Id);
			var warnings = new List<string>();
			var mapped = new HashSet<Guid>();
			var tables = new List<RoleTablePrivileges>();
			tableFilter = string.IsNullOrWhiteSpace(tableFilter) ? null : tableFilter.Trim();
			privilegeFilter = string.IsNullOrWhiteSpace(privilegeFilter) ? null : privilegeFilter.Trim();

			int? Level(Guid id)
			{
				if (!assigned.TryGetValue(id, out var grant)) return 0;
				var level = grant.Depth switch
				{
					PrivilegeDepth.Basic => 1,
					PrivilegeDepth.Local => 2,
					PrivilegeDepth.Deep => 3,
					PrivilegeDepth.Global => 4,
					_ => (int?)null
				};
				if (!level.HasValue)
					warnings.Add($"Privilege '{id}' has unknown depth '{grant.Depth}'; shown as ?.");
				return level;
			}

			foreach (var metadata in metadataResponse.EntityMetadata ?? [])
			{
				var tableActions = (metadata.Privileges ?? []).Where(privilege => Actions.Contains(privilege.PrivilegeType)).ToList();
				foreach (var privilege in tableActions) mapped.Add(privilege.PrivilegeId);
				foreach (var privilege in (metadata.Privileges ?? []).Where(privilege => privilege.PrivilegeType != PrivilegeType.None && !Actions.Contains(privilege.PrivilegeType)))
					warnings.Add($"Privilege '{privilege.Name}' on '{metadata.LogicalName}' has unclassified action '{privilege.PrivilegeType}'.");
				if (tableActions.Count == 0) continue;
				var hasAssignments = tableActions.Any(privilege => assigned.ContainsKey(privilege.PrivilegeId));
				if (!MatchesMode(mode, hasAssignments)) continue;
				var displayName = metadata.DisplayName?.UserLocalizedLabel?.Label ?? string.Empty;
				if (tableFilter != null && !Contains(metadata.LogicalName, tableFilter) && !Contains(metadata.SchemaName, tableFilter) && !Contains(displayName, tableFilter)) continue;

				var cells = Actions.Select(action =>
				{
					var candidates = tableActions.Where(privilege => privilege.PrivilegeType == action).ToList();
					if (candidates.Count == 0) return new RolePrivilegeCell(action, null, null, false, null, false);
					if (candidates.Select(privilege => privilege.PrivilegeId).Distinct().Count() > 1)
						warnings.Add($"Multiple privileges map to '{metadata.LogicalName}/{action}'; showing the widest assigned depth.");
					var privilege = candidates.OrderByDescending(candidate => assigned.TryGetValue(candidate.PrivilegeId, out var grant) ? (int)grant.Depth : -1).First();
					var name = privilege.Name ?? (catalogById.TryGetValue(privilege.PrivilegeId, out var entry) ? entry.name : privilege.PrivilegeId.ToString());
					var matches = privilegeFilter == null || Contains(name, privilegeFilter) || Contains(action.ToString(), privilegeFilter) || Contains(ActionLabel(action), privilegeFilter);
					return new RolePrivilegeCell(action, privilege.PrivilegeId, name, assigned.ContainsKey(privilege.PrivilegeId), Level(privilege.PrivilegeId), matches);
				}).ToArray();
				if (privilegeFilter == null || cells.Any(cell => cell.PrivilegeId.HasValue && cell.MatchesFilter))
					tables.Add(new RoleTablePrivileges(metadata.LogicalName ?? string.Empty, displayName, cells));
			}

			var miscellaneous = new List<RoleMiscellaneousPrivilege>();
			if (tableFilter == null)
			{
				foreach (var privilege in catalog.Where(privilege => !mapped.Contains(privilege.Id)))
				{
					var isAssigned = assigned.ContainsKey(privilege.Id);
					if (MatchesMode(mode, isAssigned) && (privilegeFilter == null || Contains(privilege.name, privilegeFilter)))
						miscellaneous.Add(new RoleMiscellaneousPrivilege(privilege.Id, privilege.name, isAssigned, Level(privilege.Id)));
				}
				foreach (var grant in assigned.Values.Where(grant => !mapped.Contains(grant.PrivilegeId) && !catalogById.ContainsKey(grant.PrivilegeId)))
				{
					var name = string.IsNullOrWhiteSpace(grant.PrivilegeName) ? grant.PrivilegeId.ToString() : grant.PrivilegeName;
					warnings.Add($"Assigned privilege '{name}' is absent from the privilege catalog; shown as unclassified.");
					if (MatchesMode(mode, true) && (privilegeFilter == null || Contains(name, privilegeFilter)))
						miscellaneous.Add(new RoleMiscellaneousPrivilege(grant.PrivilegeId, name, true, Level(grant.PrivilegeId)));
				}
			}

			return new RolePrivilegeSnapshot(
				tables.OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase).ToArray(),
				miscellaneous.OrderBy(privilege => privilege.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
				warnings.Distinct().ToArray());
		}

		public static string ActionLabel(PrivilegeType action) => action == PrivilegeType.AppendTo ? "Append To" : action.ToString();
		private static bool Contains(string? value, string filter) => value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;
		private static bool MatchesMode(RolePrivilegeViewMode mode, bool assigned) => mode == RolePrivilegeViewMode.All || (mode == RolePrivilegeViewMode.Assigned ? assigned : !assigned);
	}
}