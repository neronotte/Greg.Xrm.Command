namespace Greg.Xrm.Command.Commands.Solution
{
	public class CreateCommandResult : CommandResult
	{
		public CreateCommandResult(Guid solutionId, Guid publisherId) : base(true)
		{
			this["Solution Id"] = solutionId;
			this["Publisher Id"] = publisherId;
		}
	}

}
