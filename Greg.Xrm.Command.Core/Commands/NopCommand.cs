namespace Greg.Xrm.Command.Commands
{
	[Command("nop", Hidden = true)]
	public class NopCommand
	{
	}

	public class NopCommandExecutor : ICommandExecutor<NopCommand>
	{
		public Task<CommandResult> ExecuteAsync(NopCommand command, CancellationToken cancellationToken)
		{
			return Task.FromResult(CommandResult.Success());
		}
	}
}
