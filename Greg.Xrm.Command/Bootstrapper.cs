using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Commands.History;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.ServiceModel;

namespace Greg.Xrm.Command
{
    public sealed class Bootstrapper
	{
		private readonly ILogger log;
		private readonly ICommandRegistry registry;
		private readonly ICommandParser parser;
		private readonly IOutput output;
		private readonly ICommandLineArguments args;
		private readonly ICommandExecutorFactory commandExecutorFactory;
		private readonly IHistoryTracker historyTracker;

		private static readonly Type[] commandsNotToTrack = new[]
		{
			typeof(HelpCommand),
			typeof(GetCommand),
			typeof(SetLengthCommand),
			typeof(ClearCommand),
		};



		public Bootstrapper(
			ILogger<Bootstrapper> logger,
			IOutput output,
			ICommandRegistry registry,
			ICommandParser parser,
			ICommandLineArguments args,
			ICommandExecutorFactory commandExecutorFactory,
			IHistoryTracker historyTracker)
		{
			this.log = logger;
			this.output = output;
			this.registry = registry;
			this.parser = parser;
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

				this.registry.InitializeFromAssembly(typeof(CommandAttribute).Assembly);





				var command = this.parser.Parse(args);
				if (command == null)
				{
					Environment.Exit(-1);
					return;
				}



				if (!IsValidCommand(command))
				{
					Environment.Exit(-1);
					return;
				}

				await TrackCommandIntoHistoryAsync(command);

				if (!GetCommandExecutor(command, out MethodInfo? method, out object? commandExecutor) || method == null || commandExecutor == null)
				{
					Environment.Exit(-1);
					return;
				}

				var result = await ExecuteCommandAsync(command, commandExecutor, method, cancellationToken);

				if (result == null)
				{
					Environment.Exit(-1);
					return;
				}

				if (!result.IsSuccess)
				{
					PrintFailure(command, result);

					Environment.Exit(-1);
					return;
				}

				if (result.IsSuccess && result.Count > 0)
				{
					PrintSuccess(result);
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







		private bool IsValidCommand(object command)
		{
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
				return false;
			}

			return true;
		}




		private async Task TrackCommandIntoHistoryAsync(object command)
		{
			if (commandsNotToTrack.Contains(command.GetType()))
				return;

			await this.historyTracker.AddAsync(args.ToArray());
		}





		private bool GetCommandExecutor(object command, out MethodInfo? method, out object? commandExecutor)
		{
			method = null;
			commandExecutor = commandExecutorFactory.CreateFor(command.GetType());
			if (commandExecutor == null)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("No command executor found for command {commandType}", command.GetType());

				return false;
			}

			var specificCommandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

			method = specificCommandExecutorType.GetMethod("ExecuteAsync");
			if (method == null)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("No ExecuteAsync method found for command executor {commandExecutorType}", specificCommandExecutorType);

				return false;
			}

			return true;
		}


		public async Task<CommandResult?> ExecuteCommandAsync(object command, object commandExecutor, MethodInfo method, CancellationToken cancellationToken)
		{
			var task = (Task<CommandResult>?)method.Invoke(commandExecutor, new[] { command, cancellationToken });
			if (task == null)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("Invalid result from command executor ExecuteAsync: {commandType}", command.GetType());

				return null;
			}

			var result = await task;
			this.output.WriteLine();

			if (task.IsFaulted)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError(task.Exception, "Error while executing command {commandType}", command.GetType());
				return null;
			}

			if (task.IsCanceled)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("Command {commandType} has been cancelled", command.GetType());
				return null;
			}

			return result;
		}




		private void PrintFailure(object command, CommandResult result)
		{
			this.output.Write(result.ErrorMessage, ConsoleColor.Red).WriteLine();
			log.LogError("Command {commandType} has error", command.GetType());

			var ex = result.Exception;
			if (ex == null) return;

			log.LogError("Fault type: {faultType}", ex.GetType());
			if (ex.GetType() != typeof(FaultException<OrganizationServiceFault>))
			{
				output
					.Write("  Exception of type '", ConsoleColor.Red)
					.Write(ex.GetType().FullName, ConsoleColor.Red)
					.Write("'. ", ConsoleColor.Red);
			}

			if (ex.InnerException != null)
			{
				output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
			}
		}





		private void PrintSuccess(CommandResult result)
		{
			var padding = result.Max(_ => _.Key.Length);
			this.output.WriteLine("Result: ");
			foreach (var kvp in result)
			{
				this.output.Write("  ").Write(kvp.Key.PadRight(padding)).Write(": ").WriteLine(kvp.Value, ConsoleColor.Yellow);
			}
		}
	}
}