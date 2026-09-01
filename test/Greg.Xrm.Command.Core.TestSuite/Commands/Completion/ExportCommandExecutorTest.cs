using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Completion
{
	[TestClass]
	public class ExportCommandExecutorTest
	{
		private static async Task<(CommandResult Result, string Text)> ExecuteAsync()
		{
			var registry = new CommandRegistry(NullLogger<CommandRegistry>.Instance, new OutputToMemory(), new Storage());
			registry.InitializeFromAssembly(typeof(ExportCommand).Assembly);

			var writer = new StringWriter();
			var console = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Ansi = AnsiSupport.No,
				Out = new AnsiConsoleOutput(writer)
			});

			var executor = new ExportCommandExecutor(console, registry);
			var result = await executor.ExecuteAsync(new ExportCommand(), CancellationToken.None);

			return (result, writer.ToString());
		}


		[TestMethod]
		public async Task OutputShouldBePureJson()
		{
			var (result, text) = await ExecuteAsync();

			Assert.IsTrue(result.IsSuccess);
			StringAssert.StartsWith(text.TrimStart(), "{");

			// must be parseable as-is, without stripping any surrounding log lines
			var doc = JObject.Parse(text);
			Assert.IsNotNull(doc["commands"]);
			Assert.IsNotNull(doc["namespaces"]);
		}


		[TestMethod]
		public async Task ExportShouldContainKnownCommandWithOptionsAndEnumValues()
		{
			var (_, text) = await ExecuteAsync();
			var doc = JObject.Parse(text);

			var commands = (JArray)doc["commands"]!;
			Assert.IsTrue(commands.Count > 50, $"Expected a reasonably large command tree, found {commands.Count} commands");

			var solutionList = commands.FirstOrDefault(c =>
				c["verbs"]!.Values<string>().SequenceEqual(new[] { "solution", "list" }));
			Assert.IsNotNull(solutionList, "The export should contain the 'solution list' command");

			var formatOption = solutionList["options"]!.FirstOrDefault(o => (string?)o["long"] == "format");
			Assert.IsNotNull(formatOption, "The 'solution list' command should expose its --format option");
			Assert.AreEqual("f", (string?)formatOption["short"]);

			var values = formatOption["values"]!.Values<string>().ToList();
			CollectionAssert.Contains(values, "Json", "The --format option should list its enum values");
		}


		[TestMethod]
		public async Task ExportShouldContainTheCompletionCommandsThemselves()
		{
			var (_, text) = await ExecuteAsync();
			var doc = JObject.Parse(text);

			var commands = (JArray)doc["commands"]!;
			var export = commands.FirstOrDefault(c =>
				c["verbs"]!.Values<string>().SequenceEqual(new[] { "completion", "export" }));

			Assert.IsNotNull(export, "The export should contain 'completion export' itself");
		}


		[TestMethod]
		public async Task ExportShouldContainNamespacesWithHelp()
		{
			var (_, text) = await ExecuteAsync();
			var doc = JObject.Parse(text);

			var namespaces = (JArray)doc["namespaces"]!;
			var pluginTrace = namespaces.FirstOrDefault(n =>
				n["verbs"]!.Values<string>().SequenceEqual(new[] { "plugin", "trace" }));

			Assert.IsNotNull(pluginTrace, "The export should contain the 'plugin trace' namespace");
			Assert.AreEqual("Read plugin trace logs", (string?)pluginTrace["help"]);
		}
	}
}
