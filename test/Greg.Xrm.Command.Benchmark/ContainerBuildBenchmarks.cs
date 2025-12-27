using Autofac;
using Autofac.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Greg.Xrm.Command.Commands.Help;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.CommandHistory;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Pluralization;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Benchmark;

/// <summary>
/// Benchmarks for measuring container build time with Autofac
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ContainerBuildBenchmarks
{
    /// <summary>
    /// Measures the time to build the Autofac container with all services registered
    /// (mirrors the actual Program.cs registration pattern)
    /// </summary>
    [Benchmark(Description = "Autofac Container Build (Full)")]
    public IContainer BuildAutofacContainer_Full()
    {
        var serviceCollection = new ServiceCollection();
        
        // Register services exactly as in Program.cs
        serviceCollection.AddSingleton<IStorage>(new Storage());
        serviceCollection.AddSingleton<ICommandLineArguments>(new CommandLineArguments(Array.Empty<string>()));
        serviceCollection.AddSingleton<ICommandRegistry, CommandRegistry>();
        serviceCollection.AddSingleton<ICommandParser, CommandParser>();
        serviceCollection.RegisterCommandExecutors(typeof(CommandAttribute).Assembly);
        serviceCollection.AddTransient<ICommandExecutorFactory, CommandExecutorFactory>();
        serviceCollection.AddTransient<IPluralizationFactory, PluralizationFactory>();
        serviceCollection.AddTransient<ISettingsRepository, SettingsRepository>();
        serviceCollection.AddTransient<IPacxProjectRepository, PacxProjectRepository>();
        serviceCollection.AddSingleton<IOrganizationServiceRepository, OrganizationServiceRepository>();
        serviceCollection.AddSingleton<IOutput, OutputToMemory>();
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
        return container;
    }

    /// <summary>
    /// Measures the time to build only the ServiceCollection (without Autofac)
    /// </summary>
    [Benchmark(Description = "ServiceCollection Build Only")]
    public ServiceProvider BuildServiceCollection_Only()
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddSingleton<IStorage>(new Storage());
        serviceCollection.AddSingleton<ICommandLineArguments>(new CommandLineArguments(Array.Empty<string>()));
        serviceCollection.AddSingleton<ICommandRegistry, CommandRegistry>();
        serviceCollection.AddSingleton<ICommandParser, CommandParser>();
        serviceCollection.RegisterCommandExecutors(typeof(CommandAttribute).Assembly);
        serviceCollection.AddTransient<IPluralizationFactory, PluralizationFactory>();
        serviceCollection.AddTransient<ISettingsRepository, SettingsRepository>();
        serviceCollection.AddTransient<IPacxProjectRepository, PacxProjectRepository>();
        serviceCollection.AddSingleton<IOrganizationServiceRepository, OrganizationServiceRepository>();
        serviceCollection.AddSingleton<IOutput, OutputToMemory>();
        serviceCollection.AddTransient<IHistoryTracker, HistoryTracker>();

        serviceCollection.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });

        return serviceCollection.BuildServiceProvider();
    }
}
