using Greg.Xrm.Command.Commands.Auth;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Greg.Xrm.Command.Commands
{
	static class Utility
	{
		public static TCommand TestParseCommand<TCommand>(params string[] args)
		{
			var log = NullLogger<CommandRegistry>.Instance;
			var output = new OutputToMemory();
			var container = new Autofac.ContainerBuilder().Build();
			var storage = new Storage();

			var registry = new CommandRegistry(log, output, container, storage);
			registry.InitializeFromAssembly(typeof(ListCommand).Assembly);

			var parser = new CommandParser(new OutputToMemory(), registry);

			var parseResult = parser.Parse(args);

			Assert.IsNotNull(parseResult, $"Parsing of arguments <{Concatenate(args)}> returned no command");
			Assert.AreEqual(typeof(TCommand), parseResult.GetType(), $"On arguments <{Concatenate(args)}> the expected command type is '{typeof(TCommand).FullName}', actual is '{parseResult.GetType().FullName}'");

			var command = (TCommand)parseResult;
			return command;
		}


		public static string Concatenate(string[] args)
		{
			return string.Join(" ", args.Select(x => WrapInQuotes(x)));
		}

		public static string WrapInQuotes(string x)
		{
			if (string.IsNullOrWhiteSpace(x)) return string.Empty;
			if (x.Contains(' ')) return $"\"{x}\"";
			return x;
		}
	}
}
