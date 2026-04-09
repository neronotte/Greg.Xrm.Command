namespace Greg.Xrm.Command.Commands.Plugin
{
	[TestClass]
	public class PluginRegisterAttributesCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredArgumentShouldWork()
		{
			var command = Utility.TestParseCommand<PluginRegisterAttributesCommand>(
				"plugin", "register-attributes",
				"--dll", "C:\\plugins\\MyPlugin.dll");

			Assert.AreEqual("C:\\plugins\\MyPlugin.dll", command.Path);
			Assert.AreEqual("devkit", command.PublisherUniqueName);
			Assert.AreEqual("Development Toolkit", command.PublisherName);
			Assert.IsFalse(command.DryRun);
			Assert.AreEqual("table", command.Format);
			Assert.AreEqual("None", command.IsolationMode);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<PluginRegisterAttributesCommand>(
				"plugin", "register-attributes",
				"--dll", "C:\\plugins\\MyPlugin.dll",
				"--solution", "MySolution",
				"--publisher", "contoso",
				"--publisher-name", "Contoso",
				"--dry-run",
				"--format", "json",
				"--isolation", "Sandbox");

			Assert.AreEqual("C:\\plugins\\MyPlugin.dll", command.Path);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual("contoso", command.PublisherUniqueName);
			Assert.AreEqual("Contoso", command.PublisherName);
			Assert.IsTrue(command.DryRun);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual("Sandbox", command.IsolationMode);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<PluginRegisterAttributesCommand>(
				"plugin", "register-attributes",
				"-d", "C:\\plugins\\MyPlugin.dll",
				"-s", "MySolution",
				"-p", "contoso",
				"-f", "json");

			Assert.AreEqual("C:\\plugins\\MyPlugin.dll", command.Path);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.AreEqual("contoso", command.PublisherUniqueName);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void ParseWithoutRequiredShouldFail()
		{
			var command = Utility.TestParseCommand<PluginRegisterAttributesCommand>(
				"plugin", "register-attributes");

			Assert.AreEqual("", command.Path);
		}
	}
}
