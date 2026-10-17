using Greg.Xrm.Command.Commands.Security.Roles;

namespace Greg.Xrm.Command.Services.Security
{
	public interface IRoleAssignmentService
	{
		Task<CommandResult> ExecuteAsync(RoleAssignmentCommand command, bool revoke, CancellationToken cancellationToken);
	}
}