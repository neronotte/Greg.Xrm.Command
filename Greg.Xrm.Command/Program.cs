using Autofac;
using Autofac.Extensions.DependencyInjection;
using Greg.Xrm.Command;
using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Commands.Table.ExportMetadata;
using Greg.Xrm.Command.Model;
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
serviceCollection.AddSingleton<ICommandLineArguments>(new CommandLineArguments(args));
serviceCollection.RegisterCommandExecutors(typeof(CommandAttribute).Assembly);
serviceCollection.AddTransient<ICommandExecutorFactory, CommandExecutorFactory>();
serviceCollection.AddTransient<IPluralizationFactory, PluralizationFactory>();
serviceCollection.AddTransient<ISettingsRepository, SettingsRepository>();
serviceCollection.AddSingleton<IOrganizationServiceRepository, OrganizationServiceRepository>();
serviceCollection.AddSingleton<IOutput, OutputToConsole>();
serviceCollection.AddTransient<IAttributeMetadataBuilderFactory, AttributeMetadataBuilderFactory>();
serviceCollection.AddTransient<IExportMetadataStrategyFactory, ExportMetadataStrategyFactory>();
serviceCollection.AddTransient<IHistoryTracker, HistoryTracker>();
serviceCollection.AddTransient<IAttributeDeletionService, AttributeDeletionService>();
serviceCollection.AddTransient<IDependencyRepository, Dependency.Repository>();
serviceCollection.AddTransient<IWorkflowRepository, Workflow.Repository>();
serviceCollection.AddTransient<IProcessTriggerRepository, ProcessTrigger.Repository>();
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
var serviceProvider = new AutofacServiceProvider(container);

var hostedService = serviceProvider.GetService<Bootstrapper>();
try
{
	hostedService?.StartAsync(CancellationToken.None).Wait();
}
catch(AggregateException ex)
{
	foreach(var inner in ex.InnerExceptions)
	{
		Console.WriteLine(inner.Message);
	}
}
catch(Exception ex)
{
	Console.WriteLine(ex.Message);
}