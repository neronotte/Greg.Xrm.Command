using System.Reflection;
using Greg.Xrm.Command.Commands.Plugin.Type;
using Greg.Xrm.Command.Commands.Table;
using Greg.Xrm.Command.Interactive;
using Greg.Xrm.Command.Parsing;
using Spectre.Console;

namespace Greg.Xrm.Command
{
	[TestClass]
	public class CommandRunnerInteractiveTest
	{
		[TestMethod]
		public void OptionalNullableBooleanCanBeSkippedWithoutSettingIt()
		{
			var property = typeof(UpdateCommand).GetProperty(nameof(UpdateCommand.Activities))!;
			var option = new OptionDefinition(property, property.GetCustomAttribute<OptionAttribute>()!, false);
			var choice = SelectFirstChoice(option);

			Assert.AreEqual("--Skip--", choice);
			var command = Utility.TestParseCommand<UpdateCommand>("table", "update", "--schemaName", "new_table");
			Assert.IsNull(command.Activities);
		}

		[TestMethod]
		public void RequiredNullableAndNonNullableBooleansKeepTheirExistingFirstChoice()
		{
			var nullable = typeof(UpdateCommand).GetProperty(nameof(UpdateCommand.Activities))!;
			var nonNullable = typeof(UnregisterCommand).GetProperty(nameof(UnregisterCommand.Force))!;

			Assert.AreEqual("true", SelectFirstChoice(new OptionDefinition(nullable, nullable.GetCustomAttribute<OptionAttribute>()!, true)));
			Assert.AreEqual("true", SelectFirstChoice(new OptionDefinition(nonNullable, nonNullable.GetCustomAttribute<OptionAttribute>()!, false)));
		}

		private static string SelectFirstChoice(OptionDefinition option)
		{
			var method = typeof(CommandRunnerInteractive).GetMethod("CreateBoolPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
			var prompt = (IPrompt<string>)method.Invoke(null, [option, "Activities"])!;
			var actualConsole = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Interactive = InteractionSupport.Yes,
				Ansi = AnsiSupport.Yes,
				Out = new AnsiConsoleOutput(new StringWriter())
			});
			var input = new Mock<IAnsiConsoleInput>();
			input.Setup(x => x.ReadKey(It.IsAny<bool>())).Returns(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
			input.Setup(x => x.ReadKeyAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
			var console = new Mock<IAnsiConsole>();
			console.SetupGet(x => x.Profile).Returns(actualConsole.Profile);
			console.SetupGet(x => x.Input).Returns(input.Object);
			console.SetupGet(x => x.Cursor).Returns(actualConsole.Cursor);
			console.SetupGet(x => x.Pipeline).Returns(actualConsole.Pipeline);

			return prompt.Show(console.Object);
		}
	}
}
