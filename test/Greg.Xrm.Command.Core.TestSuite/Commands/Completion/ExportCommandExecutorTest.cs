using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
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
		public async Task ExportShouldSkipCommandsUnderHiddenNamespaces()
		{
			var (_, text) = await ExecuteAsync();
			var doc = JObject.Parse(text);

			// "!config" is a hidden namespace: the help does not show it, so the
			// completion must not offer "!config" as a candidate after "pacx" either
			var commands = (JArray)doc["commands"]!;
			var hidden = commands.Where(c => ((string?)c["verbs"]![0])!.StartsWith('!')).ToList();
			Assert.AreEqual(0, hidden.Count, $"Commands under a hidden namespace must not be exported, found: {string.Join(", ", hidden.Select(c => string.Join(' ', c["verbs"]!.Values<string>())))}");

			var namespaces = (JArray)doc["namespaces"]!;
			Assert.IsFalse(namespaces.Any(n => ((string?)n["verbs"]![0])!.StartsWith('!')));
		}


		[TestMethod]
		public async Task ExportShouldBePlainAscii()
		{
			var (_, text) = await ExecuteAsync();

			// help texts contain characters like "→"; with stdout redirected the console
			// code page may replace them with control characters that break JSON parsers,
			// so the export escapes everything outside ASCII
			var offending = text.Where(c => c > 127).Distinct().ToList();
			Assert.AreEqual(0, offending.Count, $"Export must be plain ASCII, found: {string.Join(" ", offending)}");
			Assert.IsNotNull(JObject.Parse(text)["commands"]);
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
