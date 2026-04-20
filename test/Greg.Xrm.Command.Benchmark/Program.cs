using BenchmarkDotNet.Running;
using Greg.Xrm.Command.Benchmark;

// Run all benchmarks
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
