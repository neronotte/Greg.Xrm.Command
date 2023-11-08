namespace Greg.Xrm.Command
{
	public interface ICommandExecutor<T>
	{
		Task ExecuteAsync(T command, CancellationToken cancellationToken);
	}
}
