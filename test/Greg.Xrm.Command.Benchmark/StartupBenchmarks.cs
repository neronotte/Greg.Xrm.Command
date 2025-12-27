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
/// Benchmarks for measuring the overall startup time, simulating "pacx help" command execution.
/// This is the closest simulation to "pacx nop" since there's no dedicated nop command.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StartupBenchmarks
{
    /// <summary>
    /// Measures the complete startup time including:
    /// - ServiceCollection registration
    /// - Autofac container build
    /// - Command registry initialization
    /// - Command parsing (simulating "help" command)
    /// </summary>
    [Benchmark(Description = "Full Startup (Container + Registry + Parse 'help')")]
    public object FullStartup_HelpCommand()
    {
        // 1. Build container (same as Program.cs)
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddSingleton<IStorage>(new Storage());
        serviceCollection.AddSingleton<ICommandLineArguments>(new CommandLineArguments(["help"]));
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

        using var container = containerBuilder.Build();

        // 2. Begin lifetime scope (as in Program.cs)
        using var scope = container.BeginLifetimeScope("activation");

        // 3. Resolve and initialize CommandRegistry
        var registry = scope.Resolve<ICommandRegistry>();
        registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);

        // 4. Parse command
        var parser = scope.Resolve<ICommandParser>();
        var (command, _) = parser.Parse("help");

        return command;
    }

    /// <summary>
    /// Measures startup with a specific command parsing (table create)
    /// </summary>
    [Benchmark(Description = "Full Startup (Container + Registry + Parse 'nop')")]
    public object FullStartup_TableCreateCommand()
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddSingleton<IStorage>(new Storage());
        serviceCollection.AddSingleton<ICommandLineArguments>(new CommandLineArguments(["nop"]));
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

        using var container = containerBuilder.Build();
        using var scope = container.BeginLifetimeScope("activation");

        var registry = scope.Resolve<ICommandRegistry>();
        registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);

        var parser = scope.Resolve<ICommandParser>();
        var (command, _) = parser.Parse("nop");

        return command;
    }

    /// <summary>
    /// Measures only the command registry initialization time
    /// </summary>
    [Benchmark(Description = "Command Registry Initialization Only")]
    public ICommandRegistry CommandRegistryInitialization()
    {
        var output = new OutputToMemory();
        var storage = new Storage();
        var log = Microsoft.Extensions.Logging.Abstractions.NullLogger<CommandRegistry>.Instance;
        
        var registry = new CommandRegistry(log, output, storage);
        registry.InitializeFromAssembly(typeof(HelpCommand).Assembly);
        
        return registry;
    }

    /// <summary>
    /// Measures parsing time only (with pre-built container and registry)
    /// </summary>
    private ICommandRegistry? _preBuiltRegistry;
    private ICommandParser? _preBuiltParser;

    [IterationSetup(Target = nameof(CommandParsingOnly))]
    public void SetupParsing()
    {
        var output = new OutputToMemory();
        var storage = new Storage();
        var log = Microsoft.Extensions.Logging.Abstractions.NullLogger<CommandRegistry>.Instance;
        
        _preBuiltRegistry = new CommandRegistry(log, output, storage);
        _preBuiltRegistry.InitializeFromAssembly(typeof(HelpCommand).Assembly);
        
        _preBuiltParser = new CommandParser(output, _preBuiltRegistry);
    }

    [Benchmark(Description = "Command Parsing Only (pre-built registry)")]
    public object CommandParsingOnly()
    {
        var (command, _) = _preBuiltParser!.Parse("table", "create", "-n", "TestTable");
        return command;
    }
}
