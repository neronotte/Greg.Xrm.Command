using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command
{
	public sealed class HostedService : IHostedService
	{
		private readonly ILogger log;
		private readonly IOutput output;
		private readonly ICommandLineArguments args;
		private readonly ICommandExecutorFactory commandExecutorFactory;
		private readonly IHistoryTracker historyTracker;

		public HostedService(
			ILogger<HostedService> logger,
			IOutput output,
			ICommandLineArguments args,
			ICommandExecutorFactory commandExecutorFactory,
			IHistoryTracker historyTracker)
		{
			this.log = logger;
			this.output = output;
			this.args = args;
			this.commandExecutorFactory = commandExecutorFactory;
			this.historyTracker = historyTracker;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (!args.Contains("--noprompt"))
			{
				this.output.Write(">>> Greg.Xrm.Command <<<", ConsoleColor.Green).WriteLine(" - Dataverse command tool", ConsoleColor.DarkGray);
				this.output.Write("Version ")
					.Write(GetType().Assembly.GetName()?.Version?.ToString() ?? "[unable to get version from assembly]")
					.WriteLine();
				this.output.Write("Online documentation: ").WriteLine("https://github.com/neronotte/Greg.Xrm.Command/wiki");
				this.output.Write("Feedback, Suggestions, Issues: ").WriteLine("https://github.com/neronotte/Greg.Xrm.Command/discussions");
				this.output.WriteLine();
			}
			else
			{
				args.Remove("--noprompt");
			}

			try
			{
				log.LogInformation("1. StartAsync has been called.");

				var parser = new CommandLineParser(this.output);
				parser.InitializeFromAssembly(typeof(CommandAttribute).Assembly);

				var command = parser.Parse(args);
				if (command == null)
				{
					Environment.Exit(-1);
					return;
				}


				var validationContext = new ValidationContext(command);
				var validationResults = new List<ValidationResult>();
				if (!Validator.TryValidateObject(command, validationContext, validationResults, true))
				{
					this.output.WriteLine("Invalid command options:", ConsoleColor.Red).WriteLine();
					foreach (var validationResult in validationResults)
					{
						this.output.Write("    ");
						this.output.WriteLine(validationResult.ErrorMessage, ConsoleColor.Red);
					}

					log.LogError("Invalid command");
					Environment.Exit(-1);
					return;
				}

				var commandsNotToTrack = new[]
				{
					typeof(HelpCommand),
					typeof(Commands.History.GetCommand),
					typeof(Commands.History.SetLengthCommand),
					typeof(Commands.History.ClearCommand),
				};

				if (!commandsNotToTrack.Contains(command.GetType()))
				{
					await this.historyTracker.AddAsync(args.ToArray());
				}


				var commandExecutor = commandExecutorFactory.CreateFor(command.GetType());
				if (commandExecutor == null)
				{
					this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
					log.LogError("No command executor found for command {commandType}", command.GetType());

					Environment.Exit(-1);
					return;
				}

				var specificCommandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

				var method = specificCommandExecutorType.GetMethod("ExecuteAsync");
				if (method == null)
				{
					this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
					log.LogError("No ExecuteAsync method found for command executor {commandExecutorType}", specificCommandExecutorType);

					Environment.Exit(-1);
					return;
				}

				var task = (Task?)method.Invoke(commandExecutor, new[] { command, cancellationToken });
				if (task == null)
				{
					this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
					log.LogError("Invalid result from command executor ExecuteAsync: {commandType}", command.GetType());

					Environment.Exit(-1);
					return;
				}

				await task;
				this.output.WriteLine();

				if (task.IsFaulted)
				{
					this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
					log.LogError(task.Exception, "Error while executing command {commandType}", command.GetType());
					Environment.Exit(-1);
					return;
				}

				if (task.IsCanceled)
				{
					this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
					log.LogError("Command {commandType} has been cancelled", command.GetType());
					Environment.Exit(-1);
					return;
				}

				log.LogInformation("Command {commandType} has been executed", command.GetType());
				Environment.Exit(0);
			}
			catch (CommandException ex)
			{
				this.output.WriteLine(ex.Message, ConsoleColor.Red).WriteLine();
				log.LogError(ex, "Unhandled error: {message}", ex.Message);

				Environment.Exit(-1);
			}
		}






		public Task StopAsync(CancellationToken cancellationToken)
		{
			this.output.WriteLine();
			this.output.WriteLine("STOP RECEIVED");

			Environment.Exit(-1);

			return Task.CompletedTask;
		}
	}
}