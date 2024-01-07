using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Greg.Xrm.Command;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Pluralization;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IStorage>(new Storage());
serviceCollection.AddSingleton<ICommandLineArguments>(new CommandLineArguments(args));
serviceCollection.AddSingleton<ICommandRegistry, CommandRegistry>();
serviceCollection.AddSingleton<ICommandParser, CommandParser>();
serviceCollection.RegisterCommandExecutors(typeof(CommandAttribute).Assembly);
serviceCollection.AddTransient<ICommandExecutorFactory, CommandExecutorFactory>();
serviceCollection.AddTransient<IPluralizationFactory, PluralizationFactory>();
serviceCollection.AddTransient<ISettingsRepository, SettingsRepository>();
serviceCollection.AddSingleton<IOrganizationServiceRepository, OrganizationServiceRepository>();
serviceCollection.AddSingleton<IOutput, OutputToConsole>();
serviceCollection.AddTransient<IHistoryTracker, HistoryTracker>();
serviceCollection.AddTransient<Bootstrapper>();

serviceCollection.AddAutofac();
serviceCollection.AddLogging(logging =>
{
	logging.ClearProviders();
	logging.AddDebug();
});


var containerBuilder = new ContainerBuilder();
containerBuilder.Populate(serviceCollection);

var container = containerBuilder.Build();

using (var scope = container.BeginLifetimeScope("activation"))
{
	try
	{
		var hostedService = scope.Resolve<Bootstrapper>();

		hostedService?.StartAsync(CancellationToken.None).Wait();
	}
	catch (AggregateException ex)
	{
		foreach (var inner in ex.InnerExceptions)
		{
			Console.WriteLine(inner.Message);
		}
	}
	catch (DependencyResolutionException ex)
	{
		Console.WriteLine(ex);
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.Message);
	}
#if DEBUG
	finally
	{
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
	}
#endif
}