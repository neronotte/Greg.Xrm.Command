namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CreateCustomApiCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "--unique-name", "nn_GregSum");
			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum");
			Assert.AreEqual("nn_GregSum", command.UniqueName);
		}

		[TestMethod]
		public void ParseDisplayNameLongShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum", "--display-name", "Greg Sum");
			Assert.AreEqual("Greg Sum", command.DisplayName);
		}

		[TestMethod]
		public void ParseDisplayNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum", "-d", "Greg Sum");
			Assert.AreEqual("Greg Sum", command.DisplayName);
		}

		[TestMethod]
		public void ParseBindingTypeShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum", "-b", "Entity");
			Assert.AreEqual(CustomApiBindingType.Entity, command.BindingType);
		}

		[TestMethod]
		public void ParseTypeFunctionShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum", "-t", "Function");
			Assert.AreEqual(CustomApiType.Function, command.Type);
		}

		[TestMethod]
		public void ParseIsPrivateShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum", "--is-private");
			Assert.IsTrue(command.IsPrivate);
		}

		[TestMethod]
		public void ParseAllowedStepTypeShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum", "-ast", "AsyncOnly");
			Assert.AreEqual(CustomApiAllowedStepType.AsyncOnly, command.AllowedStepType);
		}

		// ponytail: PACX parser uses Dictionary<string,string>; repeated -p/-r flags would
		// duplicate the key and throw. Use comma-separated values instead.

		[TestMethod]
		public void ParseParamsShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>(
				"customapi", "create", "-n", "nn_GregSum",
				"-p", "nn_Addend1:Integer,nn_Addend2:Integer");

			Assert.AreEqual("nn_Addend1:Integer,nn_Addend2:Integer", command.Params);
		}

		[TestMethod]
		public void ParseResponsesShouldWork()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>(
				"customapi", "create", "-n", "nn_GregSum",
				"-r", "nn_Result:Integer");

			Assert.AreEqual("nn_Result:Integer", command.Responses);
		}

		[TestMethod]
		public void DefaultValuesShouldBeSetCorrectly()
		{
			var command = Utility.TestParseCommand<CreateCustomApiCommand>("customapi", "create", "-n", "nn_GregSum");

			Assert.IsNull(command.DisplayName);
			Assert.AreEqual(string.Empty, command.Description);
			Assert.AreEqual(CustomApiBindingType.Global, command.BindingType);
			Assert.AreEqual(CustomApiType.Action, command.Type);
			Assert.IsFalse(command.IsPrivate);
			Assert.AreEqual(CustomApiAllowedStepType.SyncAndAsync, command.AllowedStepType);
			Assert.AreEqual(string.Empty, command.ExecutePrivilegeName);
			Assert.IsNull(command.Params);
			Assert.IsNull(command.Responses);
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenParamSpecIsValid()
		{
			var command = new CreateCustomApiCommand
			{
				UniqueName = "nn_GregSum",
				Params = "Addend1:Integer,Comment?:String"
			};
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenParamSpecHasSpaces()
		{
			// Spaces around commas must be trimmed before validation
			var command = new CreateCustomApiCommand
			{
				UniqueName = "nn_GregSum",
				Params = "Addend1:Integer, Addend2:Integer , Comment?:String"
			};
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_ShouldFail_WhenParamSpecIsInvalid()
		{
			var command = new CreateCustomApiCommand
			{
				UniqueName = "nn_GregSum",
				Params = "BadSpec"
			};
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(1, results.Count);
			StringAssert.Contains(results[0].ErrorMessage, "BadSpec");
		}

		[TestMethod]
		public void Validate_ShouldFail_WhenResponseSpecHasUnknownType()
		{
			var command = new CreateCustomApiCommand
			{
				UniqueName = "nn_GregSum",
				Responses = "Result:Unknowntype"
			};
			var results = command.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(command)).ToList();
			Assert.AreEqual(1, results.Count);
		}
	}
}
