using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISystemUserRepository systemUserRepository) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.Write("Retrieving users...");
				var options = new SystemUserSearchOptions(command.IncludeDisabled, command.IncludeApplicationUsers, command.Top);
				var users = await systemUserRepository.SearchAsync(crm, command.Name, options, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);

				output.WriteLine(users.Count == 1 ? "Found 1 user." : $"Found {users.Count} users.");

				var result = CommandResult.Success();
				result["Count"] = users.Count;
				if (users.Count == 0)
				{
					return result;
				}

				output.WriteTable(users, () => ["Id", "Domain Name", "First Name", "Last Name", "Business Unit"], user => [
					user.Id.ToString(),
					user.DomainName,
					user.FirstName,
					user.LastName,
					user.BusinessUnitName]);

				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine(" Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
