using Greg.Xrm.Command.Interactive;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.CommandHistory;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace Greg.Xrm.Command
{
	[TestClass]
	public class CommandRunnerFactoryTest
	{
		private static CommandRunnerFactory CreateFactory(params string[] argValues)
		{
			var args = new CommandLineArguments(argValues);
			return new CommandRunnerFactory(
				new Mock<IOutput>().Object,
				new Mock<IAnsiConsole>().Object,
				NullLogger<CommandRunnerFactory>.Instance,
				new Mock<ICommandRegistry>().Object,
				new Mock<ICommandParser>().Object,
				new Mock<ICommandExecutorFactory>().Object,
				new Mock<IHistoryTracker>().Object,
				args);
		}

		[TestMethod]
		public void CreateCommandRunner_WithNoArgs_ShouldReturnCliRunner()
		{
			var factory = CreateFactory();
			var runner = factory.CreateCommandRunner();
			Assert.IsInstanceOfType<CommandRunnerCli>(runner);
		}

		[TestMethod]
		public void CreateCommandRunner_WithInteractiveOnly_ShouldReturnInteractiveRunner()
		{
			var factory = CreateFactory("--interactive");
			var runner = factory.CreateCommandRunner();
			Assert.IsInstanceOfType<CommandRunnerInteractive>(runner);
		}

		/// <summary>
		/// Regression test for issue #189:
		/// pacx -env test --interactive was incorrectly rejected with
		/// "The --interactive flag cannot be used with other arguments."
		/// </summary>
		[TestMethod]
		public void CreateCommandRunner_WithEnvShortNameAndInteractive_ShouldReturnInteractiveRunner()
		{
			var factory = CreateFactory("-env", "test", "--interactive");
			var runner = factory.CreateCommandRunner();
			Assert.IsInstanceOfType<CommandRunnerInteractive>(runner,
				"Interactive runner should be returned when --interactive and -env are used together.");
		}

		/// <summary>
		/// Regression test for issue #189:
		/// pacx --environment test --interactive was incorrectly rejected.
		/// </summary>
		[TestMethod]
		public void CreateCommandRunner_WithEnvironmentLongNameAndInteractive_ShouldReturnInteractiveRunner()
		{
			var factory = CreateFactory("--environment", "test", "--interactive");
			var runner = factory.CreateCommandRunner();
			Assert.IsInstanceOfType<CommandRunnerInteractive>(runner,
				"Interactive runner should be returned when --interactive and --environment are used together.");
		}

		[TestMethod]
		public void CreateCommandRunner_WithInteractiveAndOtherArg_ShouldReturnNoOpRunner()
		{
			var factory = CreateFactory("--interactive", "auth");
			var runner = factory.CreateCommandRunner();
			Assert.IsInstanceOfType<CommandRunnerNoOp>(runner,
				"NoOp runner should be returned when --interactive is combined with non-environment args.");
		}
	}
}
