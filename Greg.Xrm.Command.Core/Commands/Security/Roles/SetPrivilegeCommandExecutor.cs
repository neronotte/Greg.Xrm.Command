using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class SetPrivilegeCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISecurityRoleService securityRoleService,
		IPrivilegeRepository privilegeRepository) : ICommandExecutor<SetPrivilegeCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SetPrivilegeCommand command, CancellationToken cancellationToken)
		{
			var level = command.Level?.Trim();
			var remove = string.IsNullOrEmpty(level) || level == "0" || string.Equals(level, "null", StringComparison.OrdinalIgnoreCase);
			var depth = ParseDepth(level);
			if (!remove && !depth.HasValue)
				return CommandResult.Fail($"Invalid privilege level '{command.Level}'. Use 1/Basic/User, 2/Local/BusinessUnit, 3/Deep/ParentChild, 4/Global/Organization, or 0/null to remove.");

			try
			{
				output.Write("Connecting to the current dataverse environment...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Resolving security role and privilege...");
				var roles = await securityRoleService.GetRolesByIdentifierAsync(crm, command.Role.Trim(), cancellationToken);
				if (roles.Count == 0)
					return CommandResult.Fail($"Security role '{command.Role}' was not found.");
				if (roles.Count != 1)
					return CommandResult.Fail($"Multiple root security roles named '{command.Role}' were found. Specify the role GUID with --role.");

				var role = roles[0];
				if (role.IsManaged)
					return CommandResult.Fail($"Security role '{role.Name}' ({role.RoleId}) is managed. Only unmanaged security roles can be modified.");

				var name = !string.IsNullOrWhiteSpace(command.Name)
					? command.Name.Trim()
					: $"prv{command.Privilege!.Trim()}{command.Table!.Trim()}";

				var privilege = await privilegeRepository.GetByNameAsync(crm, name, cancellationToken);
				if (privilege == null)
					return CommandResult.Fail($"Privilege '{name}' was not found in the Dataverse privilege table.");
				
                if (!remove && !privilege.Supports(depth!.Value))
				{
					return CommandResult.Fail($"Level '{depth}' is not supported by privilege '{privilege.name}'. Allowed levels: {string.Join(", ", privilege.GetAllowedLevels())}; null (remove).");
				}
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write(remove ? "Removing privilege..." : "Setting privilege level...");
				if (remove)
				{
					await crm.ExecuteAsync(new RemovePrivilegeRoleRequest { RoleId = role.RoleId, PrivilegeId = privilege.Id }, cancellationToken);
				}
				else
				{
					await crm.ExecuteAsync(new AddPrivilegesRoleRequest
					{
						RoleId = role.RoleId,
						Privileges = [new RolePrivilege { PrivilegeId = privilege.Id, Depth = depth!.Value }]
					}, cancellationToken);
				}
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["RoleId"] = role.RoleId;
				result["RoleName"] = role.Name;
				result["PrivilegeId"] = privilege.Id;
				result["PrivilegeName"] = privilege.name;
				result["Level"] = remove ? "None" : depth!.Value.ToString();
				result["Removed"] = remove;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static PrivilegeDepth? ParseDepth(string? level) => level?.ToLowerInvariant() switch
		{
			"1" or "basic" or "user" => PrivilegeDepth.Basic,
			"2" or "local" or "businessunit" => PrivilegeDepth.Local,
			"3" or "deep" or "parentchild" => PrivilegeDepth.Deep,
			"4" or "global" or "organization" => PrivilegeDepth.Global,
			_ => null
		};
	}
}