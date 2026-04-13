using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Greg.Xrm.Command.Commands.Auth;
using Greg.Xrm.Command.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greg.Xrm.Command.Benchmarks
{
	/// <summary>
	/// Benchmark suite for PACX hot paths: command parsing and output formatting.
	/// Run with: dotnet run --configuration Release --project Greg.Xrm.Command.Core.Benchmarks
	/// </summary>
	public class Program
	{
		public static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<CommandLineParserBenchmarks>();
		}
	}

	[MemoryDiagnoser]
	public class CommandLineParserBenchmarks
	{
		private CommandParser? _parser;
		private CommandRegistry? _registry;

		[GlobalSetup]
		public void Setup()
		{
			var log = NullLogger<CommandRegistry>.Instance;
			var output = new OutputToMemory();
			var storage = new Storage();
			_registry = new CommandRegistry(log, output, storage);
			_registry.InitializeFromAssembly(typeof(ListCommand).Assembly);
			_parser = new CommandParser(new OutputToMemory(), _registry);
		}

		[Benchmark]
		public void ParseSimpleCommand()
		{
			_!.Parse("auth", "list");
		}

		[Benchmark]
		public void ParseCommandWithOptions()
		{
			_!.Parse("auth", "create", "--name", "test", "--url", "https://test.crm.dynamics.com");
		}

		[Benchmark]
		public void ParseInvalidCommand()
		{
			_!.Parse("nonexistent", "command", "with", "many", "args");
		}

		[Benchmark]
		public void ParseWithSpecialCharacters()
		{
			_!.Parse("auth", "create", "--name", "test&special|chars", "--url", "https://test.crm.dynamics.com?param=value");
		}
	}

	[MemoryDiagnoser]
	public class OutputFormattingBenchmarks
	{
		private OutputToMemory _output = null!;

		[GlobalSetup]
		public void Setup()
		{
			_output = new OutputToMemory();
		}

		[Benchmark]
		public void WriteLine_Simple()
		{
			_output.WriteLine("Simple output line");
		}

		[Benchmark]
		public void WriteLine_WithData()
		{
			for (int i = 0; i < 100; i++)
			{
				_output.WriteLine($"Item {i}: Name={Guid.NewGuid()}, Value={i * 3.14}");
			}
		}

		[Benchmark]
		public void Write_TableFormat()
		{
			for (int i = 0; i < 50; i++)
			{
				_output.WriteLine($"| Column{i} | Data{i} | Value{i} |");
			}
		}
	}
}
