namespace Greg.Xrm.Command
{
	public interface ICommandExecutorFactory
	{
		object? CreateFor(Type commandType);
	}
}
