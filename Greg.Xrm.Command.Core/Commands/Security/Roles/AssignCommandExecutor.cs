namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class AssignCommandExecutor(RoleAssignmentService service) : ICommandExecutor<AssignCommand>
	{
		public Task<CommandResult> ExecuteAsync(AssignCommand command, CancellationToken cancellationToken)
		{
			return service.ExecuteAsync(command, false, cancellationToken);
		}
	}
}