using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command
{
	public sealed class HostedService : IHostedService
	{
		private readonly ILogger log;
		private readonly IOutput output;
		private readonly ICommandLineArguments args;
		private readonly ICommandExecutorFactory commandExecutorFactory;

		public HostedService(
			ILogger<HostedService> logger,
			IOutput output,
			ICommandLineArguments args,
			ICommandExecutorFactory commandExecutorFactory)
		{
			this.log = logger;
			this.output = output;
			this.args = args;
			this.commandExecutorFactory = commandExecutorFactory;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (!args.Contains("--noprompt"))
			{
				this.output.WriteLine("Greg.Xrm.Command - Dataverse command tool");
				this.output.Write("Version ")
					.Write(GetType().Assembly.GetName()?.Version?.ToString() ?? "[unable to get version from assembly]")
					.WriteLine()
					.WriteLine();
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
					this.output.WriteLine("Invalid command", ConsoleColor.Red).WriteLine();
					log.LogError("Invalid command");

					Environment.Exit(-1);
					return;
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
					log.LogError("Invalid result from command executor ExecuteAsync", command.GetType());

					Environment.Exit(-1);
					return;
				}

				await task;

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
				log.LogError(ex, ex.Message);

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