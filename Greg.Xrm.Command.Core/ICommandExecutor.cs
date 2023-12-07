namespace Greg.Xrm.Command
{
	public interface ICommandExecutor<T>
	{
		Task<CommandResult> ExecuteAsync(T command, CancellationToken cancellationToken);
	}
}
