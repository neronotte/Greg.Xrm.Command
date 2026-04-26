using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Updates;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command
{
	sealed class Bootstrapper(
		ILogger<Bootstrapper> logger,
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ICommandRegistry registry,
		ICommandRunnerFactory commandRunnerFactory,
		ICommandLineArguments args,
		IAutoUpdater updater)
	{
		private readonly ILogger log = logger;

		public async Task<int> StartAsync(CancellationToken cancellationToken)
		{
			await updater.CheckForUpdatesAsync();
			await ShowTitleBanner();

			log.LogTrace("1. StartAsync has been called.");

			registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);
			registry.ScanPluginsFolder(args);

			var commandRunner = commandRunnerFactory.CreateCommandRunner();
			var result = await commandRunner.RunCommandAsync(cancellationToken);

			await updater.LaunchUpdateAsync();
			return result;
		}






		private async Task ShowTitleBanner()
		{
			if (args.Contains("--noprompt") || args.Contains("--nologo"))
			{
				args.Remove("--noprompt");
				args.Remove("--nologo");
				return;
			}

			output.Write(">>> Greg PowerPlatform CLI Extended (PACX) <<<", ConsoleColor.Green)
				.WriteLine(" - Dataverse command tool", ConsoleColor.DarkGray);
			output.Write("Version ")
				.Write(updater.CurrentVersion);


			if (updater.UpdateRequired)
			{
				output.Write(" - New version available (will be installed on exit): ", ConsoleColor.Yellow)
					.Write(updater.NextVersion, ConsoleColor.Yellow);
			}

			output.WriteLine();
			output.Write("Online documentation: ").WriteLine("https://github.com/neronotte/Greg.Xrm.Command/wiki");
			output.Write("Feedback, Suggestions, Issues: ").WriteLine("https://github.com/neronotte/Greg.Xrm.Command/discussions");

			var connectionName = await organizationServiceRepository.GetCurrentConnectionNameAsync();
			output.Write("Current Environment: ")
				.Write(connectionName, ConsoleColor.Cyan)
				.WriteLine();

			output.WriteLine();
		}
	}
}
