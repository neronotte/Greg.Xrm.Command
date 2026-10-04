namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class ClearPrivilegeCommandExecutor(
		ICommandExecutor<SetPrivilegeCommand> setPrivilegeExecutor) : ICommandExecutor<ClearPrivilegeCommand>
	{
		public Task<CommandResult> ExecuteAsync(ClearPrivilegeCommand command, CancellationToken cancellationToken)
		{
			return setPrivilegeExecutor.ExecuteAsync(command.ToSetPrivilegeCommand(), cancellationToken);
		}
	}
}