using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class RevokeCommandExecutorTest : RoleAssignmentCommandExecutorTestBase
	{
		protected override bool Revoke => true;
		protected override RoleAssignmentCommand CreateCommand() => new RevokeCommand();
		protected override Task<CommandResult> RunAsync(RoleAssignmentService service, RoleAssignmentCommand command, CancellationToken cancellationToken)
		{
			return new RevokeCommandExecutor(service).ExecuteAsync((RevokeCommand)command, cancellationToken);
		}
	}
}