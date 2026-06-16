namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class RemoveCustomApiResponseCommandTest
	{
		[TestMethod]
		public void ParseApiLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiResponseCommand>("customapi", "remove-response", "--api", "nn_GregSum", "-n", "nn_Result");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseApiShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiResponseCommand>("customapi", "remove-response", "-a", "nn_GregSum", "-n", "nn_Result");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseResponseNameLongShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiResponseCommand>("customapi", "remove-response", "-a", "nn_GregSum", "--name", "nn_Result");
			Assert.AreEqual("nn_Result", command.ResponseUniqueName);
		}

		[TestMethod]
		public void ParseResponseNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveCustomApiResponseCommand>("customapi", "remove-response", "-a", "nn_GregSum", "-n", "nn_Result");
			Assert.AreEqual("nn_Result", command.ResponseUniqueName);
		}
	}
}
