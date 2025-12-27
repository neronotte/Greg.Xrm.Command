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
/// Benchmarks specifically for measuring child lifetime scope creation with dynamic registrations.
/// This is a key Autofac feature used by CommandExecutorFactory.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ChildScopeBenchmarks
{
    private IContainer _container = null!;
    private ICommandRegistry _registry = null!;

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

        // Initialize registry
        using var scope = _container.BeginLifetimeScope();
        _registry = scope.Resolve<ICommandRegistry>();
        _registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _container?.Dispose();
    }

    /// <summary>
    /// Measures creating a simple child scope (no dynamic registrations)
    /// </summary>
    [Benchmark(Description = "Child Scope (Simple)")]
    public void CreateSimpleChildScope()
    {
        using var scope = _container.BeginLifetimeScope("simple");
    }

    /// <summary>
    /// Measures creating a child scope with dynamic module registration
    /// (similar to CommandExecutorFactory pattern)
    /// </summary>
    [Benchmark(Description = "Child Scope (With Module Registration)")]
    public void CreateChildScopeWithModules()
    {
        using var scope = _container.BeginLifetimeScope("executor", builder =>
        {
            foreach (var module in _registry.Modules)
            {
                builder.RegisterModule(module);
            }
        });
    }

    /// <summary>
    /// Measures creating a child scope with dynamic assembly scanning
    /// (similar to CommandExecutorFactory pattern)
    /// </summary>
    [Benchmark(Description = "Child Scope (With Assembly Scanning)")]
    public void CreateChildScopeWithAssemblyScanning()
    {
        var assembly = typeof(HelpCommand).Assembly;
        
        using var scope = _container.BeginLifetimeScope("executor", builder =>
        {
            builder
                .RegisterAssemblyTypes(assembly)
                .Where(t => t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)))
                .AsSelf()
                .AsImplementedInterfaces();
        });
    }

    /// <summary>
    /// Measures the full CommandExecutorFactory pattern
    /// </summary>
    [Benchmark(Description = "Child Scope (Full CommandExecutorFactory Pattern)")]
    public void CreateChildScopeFullPattern()
    {
        var assembly = typeof(HelpCommand).Assembly;
        
        using var scope = _container.BeginLifetimeScope("executor", builder =>
        {
            foreach (var module in _registry.Modules)
            {
                builder.RegisterModule(module);
            }

            builder
                .RegisterAssemblyTypes(assembly)
                .Where(t => t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)))
                .AsSelf()
                .AsImplementedInterfaces();
        });
    }

    /// <summary>
    /// Measures resolving a command executor through the factory
    /// </summary>
    [Benchmark(Description = "CommandExecutorFactory.CreateFor")]
    public object? ResolveCommandExecutorViaFactory()
    {
        using var scope = _container.BeginLifetimeScope("activation");
        var factory = scope.Resolve<ICommandExecutorFactory>();
        var executor = factory.CreateFor(typeof(HelpCommand));
        return executor;
    }
}
