using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command
{
	class CommandRunnerCli(
			IOutput output,
			ILogger log,
			ICommandParser parser,
			ICommandExecutorFactory commandExecutorFactory,
			IHistoryTracker historyTracker,
			ICommandLineArguments args)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
		: CommandRunnerBase(output, log, commandExecutorFactory, historyTracker, args), ICommandRunner
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
	{
		public async Task<int> RunCommandAsync(CancellationToken cancellationToken)
		{
			var (command, _) = parser.Parse(args);
			if (command == null)
			{
				return -1;
			}

			if (!IsValidCommand(command))
			{
				return -1;
			}

			await TrackCommandIntoHistoryAsync(command);

			var result = await ExecuteCommand(command, cancellationToken);

			return result;
		}
	}
}
