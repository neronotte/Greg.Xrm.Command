namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class AddCustomApiResponseCommandTest
	{
		[TestMethod]
		public void ParseApiShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiResponseCommand>("customapi", "add-response", "-a", "nn_GregSum", "-r", "nn_Result:Integer");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseApiLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiResponseCommand>("customapi", "add-response", "--api", "nn_GregSum", "-r", "nn_Result:Integer");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseResponseShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiResponseCommand>("customapi", "add-response", "-a", "nn_GregSum", "-r", "nn_Result:Decimal");
			Assert.AreEqual("nn_Result:Decimal", command.Response);
		}

		[TestMethod]
		public void ParseResponseLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiResponseCommand>("customapi", "add-response", "-a", "nn_GregSum", "--response", "nn_Result:Decimal");
			Assert.AreEqual("nn_Result:Decimal", command.Response);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<AddCustomApiResponseCommand>("customapi", "add-response", "-a", "nn_GregSum", "-r", "nn_Result:Integer");
			Assert.IsNull(command.DisplayName);
			Assert.AreEqual(string.Empty, command.Description);
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenResponseSpecIsValid()
		{
			var command = new AddCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", Response = "nn_Result:Integer" };
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_ShouldFail_WhenResponseSpecIsInvalid()
		{
			var command = new AddCustomApiResponseCommand { ApiUniqueName = "nn_GregSum", Response = "MissingColon" };
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(1, results.Count);
		}
	}
}
