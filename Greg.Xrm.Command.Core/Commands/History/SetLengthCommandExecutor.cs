using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.History
{
	public class SetLengthCommandExecutor : ICommandExecutor<SetLengthCommand>
	{
		private readonly IOutput output;
		private readonly IHistoryTracker historyTracker;

		public SetLengthCommandExecutor(IOutput output, IHistoryTracker historyTracker)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.historyTracker = historyTracker ?? throw new ArgumentNullException(nameof(historyTracker));
		}


		public async Task<CommandResult> ExecuteAsync(SetLengthCommand command, CancellationToken cancellationToken)
		{
			this.output.Write("Setting command history max length to ").Write(command.Length).Write("...");

			await this.historyTracker.SetMaxLengthAsync(command.Length);

			this.output.WriteLine("Done", ConsoleColor.Green);
			return CommandResult.Success();
		}
	}
}
