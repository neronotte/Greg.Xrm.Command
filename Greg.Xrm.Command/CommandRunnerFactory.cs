using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Greg.Xrm.Command
{
	class CommandRunnerFactory(
			IOutput output,
			IAnsiConsole ansiConsole,
			ILogger<CommandRunnerFactory> log,
			ICommandRegistry registry,
			ICommandParser parser,
			ICommandExecutorFactory commandExecutorFactory,
			IHistoryTracker historyTracker,
			ICommandLineArguments args) : ICommandRunnerFactory
	{
		public ICommandRunner CreateCommandRunner()
		{
			if (args.Count == 0)
			{
				return Cli();
			}

			if (args.Contains("--interactive"))
			{
				if (args.Count == 1)
				{
					return Interactive();
				}
				else
				{
					output.WriteLine("The --interactive flag cannot be used with other arguments.", ConsoleColor.Red);
					return NoOp(-1);
				}
			}

			return Cli();
		}

		private ICommandRunner Cli() => new CommandRunnerCli(output, log, parser, commandExecutorFactory, historyTracker, args);

		private ICommandRunner Interactive() => new Interactive.CommandRunnerInteractive(output, ansiConsole, log, registry, commandExecutorFactory, historyTracker, args);

		private ICommandRunner NoOp(int result) => new CommandRunnerNoOp(result);
	}
}
