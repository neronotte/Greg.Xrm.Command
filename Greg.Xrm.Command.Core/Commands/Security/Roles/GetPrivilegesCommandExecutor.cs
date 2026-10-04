using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Newtonsoft.Json;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class GetPrivilegesCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISecurityRoleService securityRoleService,
		IRolePrivilegeInspectionService inspectionService) : ICommandExecutor<GetPrivilegesCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetPrivilegesCommand command, CancellationToken cancellationToken)
		{
			try
			{
				var format = command.OutputFormat!.Value;
				var isJson = format is RolePrivilegeOutputFormat.JsonTechnical or RolePrivilegeOutputFormat.JsonFunctional or RolePrivilegeOutputFormat.JsonNumeric;
				if (!isJson) output.Write("Connecting to the current dataverse environment...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				if (!isJson) output.WriteLine("Done", ConsoleColor.Green);
				var roles = await securityRoleService.GetRolesByIdentifierAsync(crm, command.Role.Trim(), cancellationToken);
				if (roles.Count == 0) return CommandResult.Fail($"Security role '{command.Role}' was not found.");
				if (roles.Count != 1) return CommandResult.Fail($"Multiple root roles named '{command.Role}' were found. Specify the role GUID with --role.");
				var role = roles[0];
				if (!isJson) output.Write("Retrieving role privileges and table metadata...");
				var snapshot = await inspectionService.InspectAsync(crm, role.RoleId, command.Mode, command.Table, command.Privilege, cancellationToken);
				if (isJson)
				{
					output.WriteLine(SerializePrivileges(snapshot, format));
					return CommandResult.Success();
				}
				output.WriteLine("Done", ConsoleColor.Green);
				if (!string.IsNullOrWhiteSpace(command.Table)) output.WriteLine($"Table filter: {command.Table}");
				if (!string.IsNullOrWhiteSpace(command.Privilege)) output.WriteLine($"Privilege filter: {command.Privilege}");
				foreach (var warning in snapshot.Warnings) output.WriteLine(warning, ConsoleColor.Yellow);
				output.WriteLine($"Role: {role.Name} ({role.RoleId}) | Business unit: {role.BusinessUnit} | {(role.IsManaged ? "Managed" : "Unmanaged")}");
				output.WriteLine();
				WriteTables(snapshot.Tables, format, !string.IsNullOrWhiteSpace(command.Privilege));
				if (string.IsNullOrWhiteSpace(command.Table))
				{
					output.WriteLine("Miscellaneous privileges");
					if (snapshot.Miscellaneous.Count == 0) output.WriteLine("No miscellaneous privileges match the selected mode and filters.");
					else output.WriteTable(snapshot.Miscellaneous, () => ["Label", "Level", "Technical name"], privilege =>
						[PrivilegeLabel(privilege.Name), LevelLabel(privilege.Level, format), privilege.Name]);
				}
				var result = CommandResult.Success();
				result["RoleId"] = role.RoleId;
				result["RoleName"] = role.Name;
				result["Show"] = command.Mode.ToString();
				result["Format"] = format.ToString().ToLowerInvariant();
				result["TableCount"] = snapshot.Tables.Count;
				result["MiscellaneousCount"] = snapshot.Miscellaneous.Count;
				result["WarningCount"] = snapshot.Warnings.Count;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception ex) { return CommandResult.Fail(ex.Message, ex); }
		}

		private void WriteTables(IReadOnlyList<RoleTablePrivileges> tables, RolePrivilegeOutputFormat format, bool hasPrivilegeFilter)
		{
			if (tables.Count == 0)
			{
				output.WriteLine("No tables match the selected mode and filters.");
				return;
			}
			if (format == RolePrivilegeOutputFormat.Compact)
				output.WriteTable(tables, () => ["Table", "CRWDATaS"], table => [table.LogicalName, string.Concat(table.Cells.Select(cell => CellLabel(cell, format)))]);
			else
			{
				var columns = Enumerable.Range(0, 8).Where(index => !hasPrivilegeFilter || tables.Any(table => table.Cells[index].MatchesFilter)).ToArray();
				output.WriteTable(tables,
					() => ["Table", .. columns.Select(index => RolePrivilegeInspectionService.ActionLabel(RolePrivilegeInspectionService.Actions[index]))],
					table => [table.LogicalName, .. columns.Select(index => CellLabel(table.Cells[index], format))]);
			}
			output.WriteLine(format == RolePrivilegeOutputFormat.Compact
				? "CRWDATaS: Create, Read, Write, Delete, Append, Append To, Assign, Share. 0=None, 1=User, 2=Business Unit, 3=Parent Child, 4=Organization."
				: "0/None: not assigned. Blank: unsupported action. -: excluded by privilege filter. ?: unknown depth.");
			if (format == RolePrivilegeOutputFormat.Compact) output.WriteLine("Blank: unsupported action. -: excluded by privilege filter. ?: unknown depth.");
		}

		private static string SerializePrivileges(RolePrivilegeSnapshot snapshot, RolePrivilegeOutputFormat format)
		{
			var privileges = snapshot.Tables.SelectMany(table => table.Cells)
				.Where(cell => cell.PrivilegeId.HasValue && cell.MatchesFilter)
				.Select(cell => (Id: cell.PrivilegeId!.Value, Name: cell.Name ?? cell.PrivilegeId.Value.ToString(), cell.Level))
				.Concat(snapshot.Miscellaneous.Select(privilege => (Id: privilege.PrivilegeId, privilege.Name, privilege.Level)));
			var payload = new SortedDictionary<string, object?>(StringComparer.Ordinal);
			foreach (var privilege in privileges.GroupBy(privilege => privilege.Id).Select(group => group.First()))
			{
				payload[privilege.Name] = !privilege.Level.HasValue ? null
					: format == RolePrivilegeOutputFormat.JsonNumeric ? privilege.Level.Value
					: LevelLabel(privilege.Level, format);
			}
			return JsonConvert.SerializeObject(payload, new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Include,
				DefaultValueHandling = DefaultValueHandling.Include
			});
		}

		private static string PrivilegeLabel(string name)
		{
			var label = name.StartsWith("prv", StringComparison.Ordinal) ? name[3..] : name;
			return string.Concat(label.Select((character, index) => index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));
		}

		private static string CellLabel(RolePrivilegeCell cell, RolePrivilegeOutputFormat format) => !cell.PrivilegeId.HasValue
			? (format == RolePrivilegeOutputFormat.Compact ? " " : string.Empty)
			: !cell.MatchesFilter ? "-" : LevelLabel(cell.Level, format);

		private static string LevelLabel(int? level, RolePrivilegeOutputFormat format)
		{
			if (!level.HasValue) return "?";
			if (format is RolePrivilegeOutputFormat.Compact or RolePrivilegeOutputFormat.Number) return level.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
			return level.Value switch
			{
				0 => "None",
				1 => format is RolePrivilegeOutputFormat.Technical or RolePrivilegeOutputFormat.JsonTechnical ? "Basic" : "User",
				2 => format is RolePrivilegeOutputFormat.Technical or RolePrivilegeOutputFormat.JsonTechnical ? "Local" : "Business Unit",
				3 => format is RolePrivilegeOutputFormat.Technical or RolePrivilegeOutputFormat.JsonTechnical ? "Deep" : "Parent Child",
				4 => format is RolePrivilegeOutputFormat.Technical or RolePrivilegeOutputFormat.JsonTechnical ? "Global" : "Organization",
				_ => "?"
			};
		}
	}
}