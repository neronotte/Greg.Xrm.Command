using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class AssignCommandExecutorTest : RoleAssignmentCommandExecutorTestBase
	{
		protected override bool Revoke => false;
		protected override RoleAssignmentCommand CreateCommand() => new AssignCommand();
		protected override Task<CommandResult> RunAsync(RoleAssignmentService service, RoleAssignmentCommand command, CancellationToken cancellationToken)
		{
			return new AssignCommandExecutor(service).ExecuteAsync((AssignCommand)command, cancellationToken);
		}
	}
}