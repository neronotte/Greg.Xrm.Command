namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class AddCustomApiParamCommandTest
	{
		[TestMethod]
		public void ParseApiLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiParamCommand>("customapi", "add-param", "--api", "nn_GregSum", "-p", "nn_X:Integer");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseApiShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiParamCommand>("customapi", "add-param", "-a", "nn_GregSum", "-p", "nn_X:Integer");
			Assert.AreEqual("nn_GregSum", command.ApiUniqueName);
		}

		[TestMethod]
		public void ParseParamLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiParamCommand>("customapi", "add-param", "-a", "nn_GregSum", "--param", "nn_X:Integer");
			Assert.AreEqual("nn_X:Integer", command.Param);
		}

		[TestMethod]
		public void ParseParamShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiParamCommand>("customapi", "add-param", "-a", "nn_GregSum", "-p", "nn_X:Integer");
			Assert.AreEqual("nn_X:Integer", command.Param);
		}

		[TestMethod]
		public void ParseDisplayNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<AddCustomApiParamCommand>("customapi", "add-param", "-a", "nn_GregSum", "-p", "nn_X:Integer", "-d", "X Value");
			Assert.AreEqual("X Value", command.DisplayName);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<AddCustomApiParamCommand>("customapi", "add-param", "-a", "nn_GregSum", "-p", "nn_X:Integer");
			Assert.IsNull(command.DisplayName);
			Assert.AreEqual(string.Empty, command.Description);
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenParamSpecIsValid()
		{
			var command = new AddCustomApiParamCommand { ApiUniqueName = "nn_GregSum", Param = "nn_X?:String" };
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_ShouldFail_WhenParamSpecIsInvalid()
		{
			var command = new AddCustomApiParamCommand { ApiUniqueName = "nn_GregSum", Param = "NoColon" };
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(1, results.Count);
		}
	}
}
