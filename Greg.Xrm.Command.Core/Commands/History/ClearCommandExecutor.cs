using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.History
{
	public class ClearCommandExecutor : ICommandExecutor<ClearCommand>
	{
		private readonly IOutput output;
		private readonly IHistoryTracker historyTracker;

		public ClearCommandExecutor(IOutput output, IHistoryTracker historyTracker)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.historyTracker = historyTracker ?? throw new ArgumentNullException(nameof(historyTracker));
		}


		public async Task ExecuteAsync(ClearCommand command, CancellationToken cancellationToken)
		{
			this.output.Write("Cleaning up command history...");

			await this.historyTracker.ClearAsync();

			this.output.WriteLine("Done", ConsoleColor.Green);
		}
	}
}
