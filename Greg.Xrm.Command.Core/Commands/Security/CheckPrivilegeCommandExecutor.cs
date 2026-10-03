using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security
{
	public class CheckPrivilegeCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		SecurityUserResolver userResolver,
		SecurityPrivilegeService securityPrivilegeService) : ICommandExecutor<CheckPrivilegeCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CheckPrivilegeCommand command, CancellationToken cancellationToken)
		{
			Guid? recordId = null;
			if (!string.IsNullOrWhiteSpace(command.RecordId))
			{
				if (!Guid.TryParse(command.RecordId, out var parsedRecordId))
				{
					return CommandResult.Fail($"Invalid record id '{command.RecordId}'.");
				}

				recordId = parsedRecordId;
			}

			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			SecurityUserInfo user;
			IReadOnlyList<SecurityPrivilegeInfo> privileges;
			try
			{
				user = await userResolver.ResolveAsync(crm, command.User, cancellationToken);

				output.WriteLine(recordId.HasValue
					? $"Checking privileges for user {user.FullName} ({user.UserId}) on record {recordId.Value} of table '{command.TableName}'..."
					: $"Checking privileges for user {user.FullName} ({user.UserId}) on table '{command.TableName}'...");

				privileges = await securityPrivilegeService.CheckPrivilegesAsync(crm, user.UserId, command.TableName, recordId, cancellationToken);
			}
			catch (CommandException ex)
			{
				return CommandResult.Fail(ex.Message);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}

			output.WriteLine(privileges.Count == 1 ? "Found 1 privilege." : $"Found {privileges.Count} privileges.");

			var result = CommandResult.Success();
			result["UserId"] = user.UserId;
			result["Count"] = privileges.Count;
			result["Privileges"] = privileges.Select(p => $"{p.Privilege} ({p.Depth})").Join(", ");
			if (privileges.Count == 0)
			{
				return result;
			}

			if (recordId.HasValue)
			{
				output.WriteTable(privileges, () => ["Access Right"], privilege => [privilege.Privilege]);
			}
			else
			{
				output.WriteTable(privileges, () => ["Privilege", "Depth"], privilege => [privilege.Privilege, privilege.Depth?.ToString() ?? string.Empty]);
			}
			return result;
		}
	}
}
