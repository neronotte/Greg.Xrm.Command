# Greg.Xrm.Command.Benchmark

This project contains performance benchmarks for measuring the DI container and startup performance of the PACX CLI tool.

## Running the Benchmarks

### Run All Benchmarks
```powershell
cd test/Greg.Xrm.Command.Benchmark
dotnet run -c Release
```

### Run Specific Benchmark Class
```powershell
# Container build benchmarks
dotnet run -c Release -- --filter *ContainerBuild*

# Service resolution benchmarks
dotnet run -c Release -- --filter *ServiceResolution*

# Startup benchmarks
dotnet run -c Release -- --filter *Startup*

# Child scope benchmarks
dotnet run -c Release -- --filter *ChildScope*
```

### Quick Run (fewer iterations for development)
```powershell
dotnet run -c Release -- --job short
```

## Benchmark Categories

### 1. ContainerBuildBenchmarks
Measures the time to build the DI container:
- **Autofac Container Build (Full)**: Complete container build as in Program.cs
- **ServiceCollection Build Only**: MSDI container build without Autofac

### 2. ServiceResolutionBenchmarks
Measures service resolution time:
- **Resolve Singleton**: Singleton service resolution (IOutput, ICommandRegistry)
- **Resolve Transient**: Transient service resolution (ISettingsRepository, IHistoryTracker)
- **Resolve Complex**: Complex service resolution (Bootstrapper with many dependencies)
- **Begin New Lifetime Scope**: Creating a new Autofac lifetime scope

### 3. StartupBenchmarks
Measures overall startup time:
- **Full Startup (help command)**: Complete startup simulating `pacx help`
- **Full Startup (table create)**: Complete startup simulating `pacx table create`
- **Command Registry Initialization**: Time to scan assemblies and build command tree
- **Command Parsing Only**: Parsing time with pre-built registry

### 4. ChildScopeBenchmarks
Measures child scope creation with dynamic registrations (key Autofac feature):
- **Child Scope (Simple)**: Basic scope creation
- **Child Scope (With Module Registration)**: Scope with IoCModule registration
- **Child Scope (With Assembly Scanning)**: Scope with assembly type scanning
- **Child Scope (Full Pattern)**: Full CommandExecutorFactory pattern
- **CommandExecutorFactory.CreateFor**: End-to-end executor resolution

## Interpreting Results

The benchmarks use BenchmarkDotNet which provides:
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation
- **Allocated**: Memory allocated per operation
- **Rank**: Relative ranking among benchmarks in the same class

## Purpose

These benchmarks help evaluate:
1. Current Autofac performance baseline
2. Whether migrating to another DI container (e.g., DryIoc, MSDI) would improve performance
3. Which operations are the most expensive during startup
4. Memory allocation patterns
