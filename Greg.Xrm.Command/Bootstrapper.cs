using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Commands.History;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;

namespace Greg.Xrm.Command
{
    public sealed class Bootstrapper
	{
		private readonly ILogger log;
		private readonly TelemetryClient client;
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
			TelemetryClient client,
			IOutput output,
			ICommandRegistry registry,
			ICommandParser parser,
			ICommandLineArguments args,
			ICommandExecutorFactory commandExecutorFactory,
			IHistoryTracker historyTracker)
		{
			this.log = logger;
			this.client = client;
			this.output = output;
			this.registry = registry;
			this.parser = parser;
			this.args = args;
			this.commandExecutorFactory = commandExecutorFactory;
			this.historyTracker = historyTracker;
		}

		public async Task<int> StartAsync(CancellationToken cancellationToken)
		{
			ShowTitleBanner();


			log.LogTrace("1. StartAsync has been called.");

			this.registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);
			this.registry.ScanPluginsFolder(args);

			var (command, runArgs) = this.parser.Parse(args);
			if (command == null)
			{
				return -1;
			}

			if (!IsValidCommand(command))
			{
				return -1;
			}

			await TrackCommandIntoHistoryAsync(command);

			using var op = this.client.StartOperation<RequestTelemetry>(string.Join(" ", runArgs.Verbs));
			op.Telemetry.Properties.Add("CommandType", command.GetType().FullName);

			var result = await ExecuteCommand(command, op, cancellationToken);

			await this.client.FlushAsync(cancellationToken);

			return result;

		}

		private async Task<int> ExecuteCommand(object command, IOperationHolder<RequestTelemetry> op, CancellationToken cancellationToken)
		{
			try
			{
				if (!GetCommandExecutor(command, out MethodInfo? method, out object? commandExecutor) || method == null || commandExecutor == null)
				{
					op.Telemetry.Success = false;
					op.Telemetry.ResponseCode = "404";
					op.Telemetry.Properties.Add("Error", "Command executor not found.");
					return -1;
				}

				var result = await ExecuteCommandAsync(command, commandExecutor, method, cancellationToken);
				op.Telemetry.Success = result?.IsSuccess ?? false;
				op.Telemetry.ResponseCode = result?.IsSuccess ?? false ? "200" : "500";

				if (result == null)
				{
					log.LogInformation("Command {CommandType} has been executed. Result is null.", command.GetType());
					op.Telemetry.Properties.Add("Error", $"Command {command.GetType()} has been executed. Result is null.");
					return -1;
				}

				if (!result.IsSuccess)
				{
					log.LogInformation("Command {CommandType} has been executed. Result is {CommandResult}.", command.GetType(), result.IsSuccess);
					PrintFailure(command, result);

					return -1;
				}

				if (result.IsSuccess && result.Count > 0)
				{
					PrintSuccess(result);
				}

				log.LogInformation("Command {CommandType} has been executed. Result is {CommandResult}", command.GetType(), result.IsSuccess);
				return 0;
			}
			catch (Exception ex)
			{
				op.Telemetry.Success = false;
				op.Telemetry.ResponseCode = "500";
				op.Telemetry.Properties.Add("Exception", ex.Message);
				op.Telemetry.Properties.Add("ExceptionType", ex.GetType().FullName);

				if (ex.InnerException != null)
				{
					op.Telemetry.Properties.Add("InnerException", ex.InnerException.Message);
					op.Telemetry.Properties.Add("InnerExceptionType", ex.InnerException.GetType().FullName);
				}

				this.output.WriteLine(ex.Message, ConsoleColor.Red).WriteLine();
				log.LogError(ex, "Unhandled error: {ErrorMessage}", ex.Message);

				return -1;
			}
		}


		private void ShowTitleBanner()
		{
			if (!args.Contains("--noprompt"))
			{
				this.output.Write(">>> Greg PowerPlatform CLI Extended (PACX) <<<", ConsoleColor.Green).WriteLine(" - Dataverse command tool", ConsoleColor.DarkGray);
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

				log.LogError("Invalid command options");
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
				log.LogError("No command executor found for command {CommandType}.", command.GetType());

				return false;
			}

			var specificCommandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

			method = specificCommandExecutorType.GetMethod("ExecuteAsync");
			if (method == null)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("No ExecuteAsync method found for command executor {CommandExecutorType}.", specificCommandExecutorType);

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
				log.LogError("Invalid result from command executor ExecuteAsync: {CommandType}.", command.GetType());

				return null;
			}

			var result = await task;
			this.output.WriteLine();

			if (task.IsFaulted)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError(task.Exception, "Error while executing command {CommandType}.", command.GetType());
				return null;
			}

			if (task.IsCanceled)
			{
				this.output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("Command {CommandType} has been cancelled.", command.GetType());
				return null;
			}

			return result;
		}




		private void PrintFailure(object command, CommandResult result)
		{
			this.output.Write(result.ErrorMessage, ConsoleColor.Red).WriteLine();
			log.LogError("Command {CommandType} has error", command.GetType());

			var ex = result.Exception;
			if (ex == null) return;

			log.LogError(ex, "Fault type: {FaultType}, {ErrorMessage}.", ex.GetType(), ex.Message);
			if (ex.InnerException != null)
			{
				log.LogError(ex.InnerException, "Inner exception: {ErrorMessage}.", ex.InnerException.Message);
			}

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