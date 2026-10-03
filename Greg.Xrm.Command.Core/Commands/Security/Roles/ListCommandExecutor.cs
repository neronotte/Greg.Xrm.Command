using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		SecurityRoleService securityRoleService) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.WriteLine(command.UnmanagedOnly ? "Retrieving unmanaged security roles..." : "Retrieving security roles...");
				var roles = await securityRoleService.GetRolesAsync(crm, command.UnmanagedOnly, command.Name, cancellationToken);

				output.WriteLine(roles.Count == 1 ? "Found 1 role." : $"Found {roles.Count} roles.");

				var result = CommandResult.Success();
				result["Count"] = roles.Count;
				result["Roles"] = roles;
				if (roles.Count == 0)
				{
					return result;
				}

				output.WriteTable(roles, () => ["Name", "Business Unit", "Type"], role => [
					role.Name,
					role.BusinessUnit,
					role.IsManaged ? "Managed" : "Unmanaged"]);

				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
