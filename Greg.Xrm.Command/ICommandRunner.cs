namespace Greg.Xrm.Command
{
	interface ICommandRunner
	{
		Task<int> RunCommandAsync(CancellationToken cancellationToken);
	}
}
