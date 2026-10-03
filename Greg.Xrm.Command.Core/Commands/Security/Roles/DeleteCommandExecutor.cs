using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class DeleteCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository connections,
		SecurityRole.Repository roles) : ICommandExecutor<DeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);
				output.Write("Resolving security role...");
				var role = await roles.GetByIdentifierAsync(crm, command.Role.Trim(), cancellationToken);
				if (role.ismanaged == true)
					return CommandResult.Fail($"Security role '{role.name}' ({role.Id}) is managed. Only unmanaged security roles can be deleted.");
				if (!role.ismanaged.HasValue)
					return CommandResult.Fail($"Unable to determine whether security role '{role.name}' ({role.Id}) is managed. Deletion was not attempted.");
				output.WriteLine("Done", ConsoleColor.Green);
				output.Write("Deleting security role ").Write(role.name, ConsoleColor.Yellow).Write("...");
				await roles.DeleteAsync(crm, role.Id, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				var result = CommandResult.Success();
				result["RoleId"] = role.Id;
				result["RoleName"] = role.name;
				result["Deleted"] = true;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception exception) { return CommandResult.Fail(exception.Message, exception); }
		}
	}
}