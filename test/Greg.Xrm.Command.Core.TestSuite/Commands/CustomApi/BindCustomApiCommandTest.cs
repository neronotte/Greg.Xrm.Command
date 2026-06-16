namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class BindCustomApiCommandTest
	{
		[TestMethod]
		public void ParseApiLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<BindCustomApiCommand>("customapi", "bind", "--api", "nn_GregSum", "-p", "MyNs.GregPlugin");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseApiShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<BindCustomApiCommand>("customapi", "bind", "-a", "nn_GregSum", "-p", "MyNs.GregPlugin");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParsePluginLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<BindCustomApiCommand>("customapi", "bind", "-a", "nn_GregSum", "--plugin", "MyNs.GregPlugin");
			Assert.AreEqual("MyNs.GregPlugin", command.PluginTypeName);
		}

		[TestMethod]
		public void ParsePluginShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<BindCustomApiCommand>("customapi", "bind", "-a", "nn_GregSum", "-p", "MyNs.GregPlugin");
			Assert.AreEqual("MyNs.GregPlugin", command.PluginTypeName);
		}

		[TestMethod]
		public void ParseAssemblyShouldWork()
		{
			var command = Utility.TestParseCommand<BindCustomApiCommand>("customapi", "bind", "-a", "nn_GregSum", "-p", "MyNs.GregPlugin", "--assembly", "MyProject.Plugins");
			Assert.AreEqual("MyProject.Plugins", command.AssemblyName);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<BindCustomApiCommand>("customapi", "bind", "-a", "nn_GregSum", "-p", "MyNs.GregPlugin");
			Assert.IsNull(command.AssemblyName);
		}
	}
}
