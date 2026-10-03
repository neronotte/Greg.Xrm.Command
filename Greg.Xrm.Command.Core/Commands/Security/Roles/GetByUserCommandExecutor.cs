using System.ServiceModel;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class GetByUserCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		SecurityUserResolver userResolver,
		SecurityRoleService securityRoleService) : ICommandExecutor<GetByUserCommand>
	{
		public async Task<CommandResult> ExecuteAsync(GetByUserCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var user = await userResolver.ResolveAsync(crm, command.User, cancellationToken);

				output.WriteLine($"Retrieving security roles for user {user.FullName} ({user.UserId})...");
				var roles = await securityRoleService.GetRolesByUserAsync(crm, user.UserId, cancellationToken);

				output.WriteLine(roles.Count == 1 ? "Found 1 role." : $"Found {roles.Count} roles.");

				var result = CommandResult.Success();
				result["UserId"] = user.UserId;
				result["Count"] = roles.Count;
				result["Roles"] = roles.Select(x => x.Role.Name).OrderBy(x => x).ToList().Join(", ");
				if (roles.Count == 0)
				{
					return result;
				}

				output.WriteTable(roles, () => ["Name", "Business Unit", "Type", "Sources"], role => [
					role.Role.Name,
					role.Role.BusinessUnit,
					role.Role.IsManaged ? "Managed" : "Unmanaged",
					string.Join(", ", role.Sources)]);

				return result;
			}
			catch (CommandException ex)
			{
				return CommandResult.Fail(ex.Message);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
