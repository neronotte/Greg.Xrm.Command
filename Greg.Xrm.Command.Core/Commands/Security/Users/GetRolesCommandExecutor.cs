using Greg.Xrm.Command.Commands.Security.Roles;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	public class GetRolesCommandExecutor(
		ICommandExecutor<GetByUserCommand> getByUserExecutor) : ICommandExecutor<GetRolesCommand>
	{
		public Task<CommandResult> ExecuteAsync(GetRolesCommand command, CancellationToken cancellationToken)
		{
			return getByUserExecutor.ExecuteAsync(command.ToGetByUserCommand(), cancellationToken);
		}
	}
}