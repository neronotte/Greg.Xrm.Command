using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using System.Globalization;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class CloneCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository connections,
		ISecurityRoleRepository roles,
		IBusinessUnitRepository businessUnits) : ICommandExecutor<CloneCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CloneCommand command, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);
				output.Write("Resolving source role and destination business unit...");
				var source = await roles.GetByIdentifierAsync(crm, command.Role.Trim(), cancellationToken);
				var businessUnitId = source.businessunitid?.Id;
				if (!string.IsNullOrWhiteSpace(command.BusinessUnit))
				{
					businessUnitId = (await businessUnits.ResolveAsync(crm, command.BusinessUnit.Trim(), cancellationToken)).Id;
				}
				if (!businessUnitId.HasValue || businessUnitId.Value == Guid.Empty)
					return CommandResult.Fail("The source role has no business unit. Specify --businessunit.");
				var inheritance = (int?)command.Inheritance ?? source.isinherited?.Value ?? 1;
				if (inheritance is not (0 or 1))
					return CommandResult.Fail($"Unsupported source member privilege inheritance value '{inheritance}'. Specify --inheritance TeamOnly or DirectUserAndTeam.");
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Selecting a unique role name...");
				var names = await roles.GetNamesAsync(crm, cancellationToken);
				var name = string.IsNullOrWhiteSpace(command.Name) ? GenerateCopyName(source.name, names) : command.Name.Trim();
				if (names.Contains(name, StringComparer.OrdinalIgnoreCase))
					return CommandResult.Fail($"A security role named '{name}' already exists in the environment. Specify a different --name.");
				output.WriteLine(name, ConsoleColor.Yellow);

				output.Write("Creating role and copying privileges in one transaction...");
				var clone = await roles.CloneAsync(crm, source.Id, name, command.Description ?? source.description,
					businessUnitId.Value, inheritance, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				var result = CommandResult.Success();
				result["SourceRoleId"] = source.Id;
				result["RoleId"] = clone.RoleId;
				result["RoleName"] = name;
				result["BusinessUnitId"] = businessUnitId.Value;
				result["Inheritance"] = ((MemberPrivilegeInheritance)inheritance).ToString();
				result["PrivilegeCount"] = clone.PrivilegeCount;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception exception) { return CommandResult.Fail(exception.Message, exception); }
		}

		private static string GenerateCopyName(string source, IReadOnlyList<string> names)
		{
			var highest = 0;
			foreach (var name in names)
			{
				if (string.Equals(name, CopyName(source, 1), StringComparison.OrdinalIgnoreCase))
					highest = Math.Max(highest, 1);
				var marker = name.LastIndexOf(" - Copy ", StringComparison.OrdinalIgnoreCase);
				if (marker >= 0 && int.TryParse(name[(marker + 8)..], NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
					number >= 2 && string.Equals(name, CopyName(source, number), StringComparison.OrdinalIgnoreCase))
					highest = Math.Max(highest, number);
			}
			return CopyName(source, checked(highest + 1));
		}

		private static string CopyName(string source, int number)
		{
			var suffix = number == 1 ? " - Copy" : " - Copy " + number.ToString(CultureInfo.InvariantCulture);
			return source[..Math.Min(source.Length, 100 - suffix.Length)] + suffix;
		}
	}
}