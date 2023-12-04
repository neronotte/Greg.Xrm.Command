using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.History
{
	public class GetCommandExecutor : ICommandExecutor<GetCommand>
	{
		private readonly IOutput output;
		private readonly IHistoryTracker historyTracker;

		public GetCommandExecutor(IOutput output, IHistoryTracker historyTracker)
        {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.historyTracker = historyTracker ?? throw new ArgumentNullException(nameof(historyTracker));
		}

        public async Task ExecuteAsync(GetCommand command, CancellationToken cancellationToken)
		{

			if (command.Length.HasValue)
			{
				this.output.Write("Retrieving last ").Write(command.Length).Write(" commands...");
			}
            else
            {
                this.output.Write("Retrieving all commands...");
            }


            var commands = await this.historyTracker.GetLastAsync(command.Length);

			this.output.WriteLine("Done", ConsoleColor.Green);

			var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

			if (commands.Count == 0)
			{
				this.output.WriteLine("No commands found", ConsoleColor.Yellow);
				return;
			}

			var i = 0;
			var padding = commands.Count.ToString().Length;
			foreach (var c in commands)
			{
				this.output.Write("  [").Write(i.ToString().PadLeft(padding)).Write("] ").Write(assemblyName).Write(" ").WriteLine(c);
				i++;
			}

			if (!string.IsNullOrWhiteSpace(command.File))
			{
				this.output.Write("Saving to file ").Write(command.File).Write("...");
				await File.WriteAllLinesAsync(command.File, commands.Select(x => $"{assemblyName} {x}"));
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
		}
	}
}
