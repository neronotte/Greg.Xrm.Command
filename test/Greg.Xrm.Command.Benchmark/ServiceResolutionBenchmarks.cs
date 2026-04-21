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
/// Benchmarks for measuring service resolution time with Autofac
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ServiceResolutionBenchmarks
{
    private IContainer _container = null!;
    private ILifetimeScope _scope = null!;

    [GlobalSetup]
    public void Setup()
    {
        var serviceCollection = new ServiceCollection();
        
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

        _container = containerBuilder.Build();
        _scope = _container.BeginLifetimeScope("benchmark");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _container?.Dispose();
    }

    [Benchmark(Description = "Resolve Singleton (IOutput)")]
    public IOutput ResolveSingleton()
    {
        return _scope.Resolve<IOutput>();
    }

    [Benchmark(Description = "Resolve Singleton (ICommandRegistry)")]
    public ICommandRegistry ResolveCommandRegistry()
    {
        return _scope.Resolve<ICommandRegistry>();
    }

    [Benchmark(Description = "Resolve Transient (ISettingsRepository)")]
    public ISettingsRepository ResolveTransient()
    {
        return _scope.Resolve<ISettingsRepository>();
    }

    [Benchmark(Description = "Resolve Transient (IHistoryTracker)")]
    public IHistoryTracker ResolveHistoryTracker()
    {
        return _scope.Resolve<IHistoryTracker>();
    }

    [Benchmark(Description = "Resolve Complex (Bootstrapper)")]
    public Bootstrapper ResolveBootstrapper()
    {
        return _scope.Resolve<Bootstrapper>();
    }

    [Benchmark(Description = "Resolve ICommandExecutorFactory")]
    public ICommandExecutorFactory ResolveCommandExecutorFactory()
    {
        return _scope.Resolve<ICommandExecutorFactory>();
    }

    [Benchmark(Description = "Begin New Lifetime Scope")]
    public ILifetimeScope BeginLifetimeScope()
    {
        using var scope = _container.BeginLifetimeScope("test");
        return scope;
    }
}
