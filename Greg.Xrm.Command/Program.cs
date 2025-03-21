﻿using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Greg.Xrm.Command;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Properties;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Pluralization;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Diagnostics;

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
serviceCollection.AddTransient<IPacxProjectRepository, PacxProjectRepository>();
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

serviceCollection.AddApplicationInsightsTelemetryWorkerService(options => 
{
#if DEBUG
	options.ConnectionString = Resources.ApplicationInsightsConnectionStringDev;
	options.DeveloperMode = true;

#elif RELEASE

	options.ConnectionString = Resources.ApplicationInsightsConnectionStringProd;
	options.DeveloperMode = true;
#endif
});


var containerBuilder = new ContainerBuilder();
containerBuilder.Populate(serviceCollection);

var container = containerBuilder.Build();

var result = -500;

using (var scope = container.BeginLifetimeScope("activation"))
{
	try
	{
		var hostedService = scope.Resolve<Bootstrapper>();
		var task = hostedService.StartAsync(CancellationToken.None);
		result = task.GetAwaiter().GetResult();
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
		if (Debugger.IsAttached)
		{
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}
	}
#endif
	return result;
}