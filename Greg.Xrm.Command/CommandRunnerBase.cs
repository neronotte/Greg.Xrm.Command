using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.ServiceModel;
using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Commands.History;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command
{
	abstract class CommandRunnerBase(
		IOutput output,
		ILogger log,
		ICommandExecutorFactory commandExecutorFactory,
		IHistoryTracker historyTracker,
		ICommandLineArguments args)
	{
		private static readonly Type[] commandsNotToTrack =
		[
			typeof(HelpCommand),
			typeof(GetCommand),
			typeof(SetLengthCommand),
			typeof(ClearCommand),
		];


		protected virtual bool IsValidCommand(object command)
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



		protected async Task TrackCommandIntoHistoryAsync(object command)
		{
			if (commandsNotToTrack.Contains(command.GetType()))
				return;

			await historyTracker.AddAsync([.. args]);
		}

		protected async Task<int> ExecuteCommand(object command, CancellationToken cancellationToken)
		{
			try
			{
				if (!GetCommandExecutor(command, out MethodInfo? method, out object? commandExecutor) || method == null || commandExecutor == null)
				{
					return -1;
				}

				var result = await ExecuteCommandAsync(command, commandExecutor, method, cancellationToken);

				if (result == null)
				{
					log.LogInformation("Command {CommandType} has been executed. Result is null.", command.GetType());
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

				if (ex.InnerException != null)
				{
					message += Environment.NewLine + " Inner exception: " + ex.InnerException.Message;
				}

				output.WriteLine(message, ConsoleColor.Red).WriteLine();
				log.LogError(ex, "Unhandled error: {ErrorMessage}", ex.Message);

				return -1;
			}
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


		protected async Task<CommandResult?> ExecuteCommandAsync(object command, object commandExecutor, MethodInfo method, CancellationToken cancellationToken)
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
	}
}
