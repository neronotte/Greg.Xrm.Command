using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class AssignCommandExecutor(IRoleAssignmentService service) : ICommandExecutor<AssignCommand>
	{
		public Task<CommandResult> ExecuteAsync(AssignCommand command, CancellationToken cancellationToken)
		{
			return service.ExecuteAsync(command, false, cancellationToken);
		}
	}
}