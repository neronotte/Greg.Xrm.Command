using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Completion
{
	[TestClass]
	public class PowerShellCommandExecutorTest
	{
		[TestMethod]
		public async Task ScriptShouldRegisterTheCompleterAndLoadTheTreeLazily()
		{
			var writer = new StringWriter();
			var console = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Ansi = AnsiSupport.No,
				Out = new AnsiConsoleOutput(writer)
			});
			var executor = new PowerShellCommandExecutor(console);

			var result = await executor.ExecuteAsync(new PowerShellCommand(), CancellationToken.None);
			var script = writer.ToString();

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(script, "Register-ArgumentCompleter -Native -CommandName pacx");
			StringAssert.Contains(script, "pacx completion export --nologo");
			StringAssert.StartsWith(script.TrimStart(), "# pacx tab-completion for PowerShell.");
		}
	}
}
