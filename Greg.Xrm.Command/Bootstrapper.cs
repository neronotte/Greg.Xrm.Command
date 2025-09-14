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
using System.Reflection;
using System.ServiceModel;

namespace Greg.Xrm.Command
{
	public sealed class Bootstrapper(
		ILogger<Bootstrapper> logger,
		TelemetryClient client,
		IOutput output,
		ICommandRegistry registry,
		ICommandParser parser,
		ICommandLineArguments args,
		ICommandExecutorFactory commandExecutorFactory,
		IHistoryTracker historyTracker)
	{
		private readonly ILogger log = logger;
		private static readonly Type[] commandsNotToTrack =
		[
			typeof(HelpCommand),
			typeof(GetCommand),
			typeof(SetLengthCommand),
			typeof(ClearCommand),
		];

		private AutoUpdater updater = new AutoUpdater(logger, output);

		public async Task<int> StartAsync(CancellationToken cancellationToken)
		{
			await this.updater.CheckForUpdates();
			ShowTitleBanner();


			log.LogTrace("1. StartAsync has been called.");

			registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);
			registry.ScanPluginsFolder(args);

			var (command, runArgs) = parser.Parse(args);
			if (command == null)
			{
				return -1;
			}

			if (!IsValidCommand(command))
			{
				return -1;
			}

			await TrackCommandIntoHistoryAsync(command);

			using var op = client.StartOperation<RequestTelemetry>(string.Join(" ", runArgs.Verbs));
			op.Telemetry.Properties.Add("CommandType", command.GetType().FullName);

			var result = await ExecuteCommand(command, op, cancellationToken);

			await client.FlushAsync(cancellationToken);

			this.updater.LaunchUpdate();
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
				var message = ex.Message;

				op.Telemetry.Success = false;
				op.Telemetry.ResponseCode = "500";
				op.Telemetry.Properties.Add("Exception", ex.Message);
				op.Telemetry.Properties.Add("ExceptionType", ex.GetType().FullName);

				if (ex.InnerException != null)
				{
					op.Telemetry.Properties.Add("InnerException", ex.InnerException.Message);
					op.Telemetry.Properties.Add("InnerExceptionType", ex.InnerException.GetType().FullName);
					message += Environment.NewLine + " Inner exception: " + ex.InnerException.Message;
				}

				output.WriteLine(message, ConsoleColor.Red).WriteLine();
				log.LogError(ex, "Unhandled error: {ErrorMessage}", ex.Message);

				return -1;
			}
		}




		private void ShowTitleBanner()
		{
			if (args.Contains("--noprompt") || args.Contains("--nologo"))
			{
				args.Remove("--noprompt");
				args.Remove("--nologo");
				return;
			}


			output.Write(">>> Greg PowerPlatform CLI Extended (PACX) <<<", ConsoleColor.Green).WriteLine(" - Dataverse command tool", ConsoleColor.DarkGray);
			output.Write("Version ")
				.Write(this.updater.CurrentVersion);

			if (this.updater.UpdateRequired)
			{
				output.Write(" - New version available (will be installed on exit): ", ConsoleColor.Yellow)
					.Write(this.updater.NextVersion, ConsoleColor.Yellow);
			}

			output.WriteLine();
			output.Write("Online documentation: ").WriteLine("https://github.com/neronotte/Greg.Xrm.Command/wiki");
			output.Write("Feedback, Suggestions, Issues: ").WriteLine("https://github.com/neronotte/Greg.Xrm.Command/discussions");
			output.WriteLine();
		}

		private bool IsValidCommand(object command)
		{
			var validationContext = new ValidationContext(command);
			var validationResults = new List<ValidationResult>();
			if (!Validator.TryValidateObject(command, validationContext, validationResults, true))
			{
				output.WriteLine("Invalid command options:", ConsoleColor.Red).WriteLine();
				foreach (var validationResult in validationResults)
				{
					output.Write("    ");
					output.WriteLine(validationResult.ErrorMessage, ConsoleColor.Red);
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

			await historyTracker.AddAsync([.. args]);
		}





		private bool GetCommandExecutor(object command, out MethodInfo? method, out object? commandExecutor)
		{
			method = null;
			commandExecutor = commandExecutorFactory.CreateFor(command.GetType());
			if (commandExecutor == null)
			{
				output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("No command executor found for command {CommandType}.", command.GetType());

				return false;
			}

			var specificCommandExecutorType = typeof(ICommandExecutor<>).MakeGenericType(command.GetType());

			method = specificCommandExecutorType.GetMethod("ExecuteAsync");
			if (method == null)
			{
				output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("No ExecuteAsync method found for command executor {CommandExecutorType}.", specificCommandExecutorType);

				return false;
			}

			return true;
		}


		public async Task<CommandResult?> ExecuteCommandAsync(object command, object commandExecutor, MethodInfo method, CancellationToken cancellationToken)
		{
			var task = (Task<CommandResult>?)method.Invoke(commandExecutor, [command, cancellationToken]);
			if (task == null)
			{
				output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("Invalid result from command executor ExecuteAsync: {CommandType}.", command.GetType());

				return null;
			}

			var result = await task;
			output.WriteLine();

			if (task.IsFaulted)
			{
				output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError(task.Exception, "Error while executing command {CommandType}.", command.GetType());
				return null;
			}

			if (task.IsCanceled)
			{
				output.WriteLine("Internal error, see logs for more info.", ConsoleColor.Red).WriteLine();
				log.LogError("Command {CommandType} has been cancelled.", command.GetType());
				return null;
			}

			return result;
		}




		private void PrintFailure(object command, CommandResult result)
		{
			output.Write(result.ErrorMessage, ConsoleColor.Red).WriteLine();
			
			var ex = result.Exception;
			if (ex == null) return;

			log.LogError(ex, "Command {CommandType} has error, Fault type: {FaultType}, {ErrorMessage}.", command.GetType(), ex.GetType(), ex.Message);
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
			output.WriteLine("Result: ");
			foreach (var kvp in result)
			{
				output.Write("  ").Write(kvp.Key.PadRight(padding)).Write(": ").WriteLine(kvp.Value, ConsoleColor.Yellow);
			}
		}
	}
}