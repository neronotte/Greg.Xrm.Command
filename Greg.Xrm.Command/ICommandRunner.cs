namespace Greg.Xrm.Command
{
	public interface ICommandRunner
	{
		Task<int> RunCommandAsync(CancellationToken cancellationToken);
	}
}
