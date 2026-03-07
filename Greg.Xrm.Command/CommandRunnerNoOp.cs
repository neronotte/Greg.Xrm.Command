namespace Greg.Xrm.Command
{
	class CommandRunnerNoOp(int result) : ICommandRunner
	{
		public Task<int> RunCommandAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(result);
		}
	}
}