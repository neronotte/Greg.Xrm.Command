using Greg.Xrm.Command;
using Greg.Xrm.Command.Services.Output;

namespace SamplePlugin
{
	public class SampleCommandExecutor : ICommandExecutor<SampleCommand>
	{
		private readonly IOutput output;

		public SampleCommandExecutor(IOutput output)
		{
			this.output = output;
		}

		public Task<CommandResult> ExecuteAsync(SampleCommand command, CancellationToken cancellationToken)
		{
			this.output.WriteLine($"Echo: {command.Echo ?? "-"}");
			return Task.FromResult(CommandResult.Success());
		}
	}
}
