using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class RevokeCommandExecutor(IRoleAssignmentService service) : ICommandExecutor<RevokeCommand>
	{
		public Task<CommandResult> ExecuteAsync(RevokeCommand command, CancellationToken cancellationToken)
		{
			return service.ExecuteAsync(command, true, cancellationToken);
		}
	}
}