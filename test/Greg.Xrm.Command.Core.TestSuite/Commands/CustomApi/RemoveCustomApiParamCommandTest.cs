namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class RemoveCustomApiParamCommandTest
	{
		[TestMethod]
		public void ParseApiLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiParamCommand>("customapi", "remove-param", "--api", "nn_GregSum", "-n", "nn_Addend1");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseApiShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiParamCommand>("customapi", "remove-param", "-a", "nn_GregSum", "-n", "nn_Addend1");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseParamNameLongShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiParamCommand>("customapi", "remove-param", "-a", "nn_GregSum", "--name", "nn_Addend1");
			Assert.AreEqual("nn_Addend1", command.ParamUniqueName);
		}

		[TestMethod]
		public void ParseParamNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiParamCommand>("customapi", "remove-param", "-a", "nn_GregSum", "-n", "nn_Addend1");
			Assert.AreEqual("nn_Addend1", command.ParamUniqueName);
		}
	}
}
