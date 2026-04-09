namespace Greg.Xrm.Command
{
	public interface ICommandExecutorFactory : IDisposable
	{
		object? CreateFor(Type commandType);
	}
}
