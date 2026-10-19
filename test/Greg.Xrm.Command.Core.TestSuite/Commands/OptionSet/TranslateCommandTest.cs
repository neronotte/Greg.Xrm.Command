using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[TestClass]
	public class TranslateCommandTest
	{
		[TestMethod]
		public void ParseWithGlobalOptionSetLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"optionset", "translate",
				"--name", "new_status",
				"--value", "100000000",
				"--labels", "1033:\"Pending\"");

			Assert.AreEqual("new_status", command.Name);
			Assert.AreEqual(100000000, command.Value);
			Assert.IsFalse(command.NoPublish);
		}

		[TestMethod]
		public void ParseWithLocalOptionSetShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"optionset", "translate",
				"-t", "account",
				"-c", "new_status",
				"-v", "100000000",
				"-l", "1033:\"Pending\"",
				"-np");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("new_status", command.ColumnName);
			Assert.IsTrue(command.NoPublish);
		}

		[TestMethod]
		public void Validate_ShouldRejectGlobalAndLocalReferencesTogether()
		{
			var command = new TranslateCommand { Name = "new_status", TableName = "account", ColumnName = "new_status" };

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.IsTrue(results.Any(result =>
				result.ErrorMessage?.Contains("cannot be combined", StringComparison.Ordinal) == true));
		}

		[TestMethod]
		public void ParseGlobalMetadataTranslationsShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"optionset", "translate",
				"--name", "new_status",
				"--displayName", "1033:\"Contract status\"",
				"--description", "1033:\"Available statuses\"");

			Assert.AreEqual("new_status", command.Name);
			Assert.AreEqual("1033:\"Contract status\"", command.DisplayName);
			Assert.AreEqual("1033:\"Available statuses\"", command.Description);
			Assert.IsNull(command.Value);
			Assert.IsNull(command.Labels);
		}
	}
}
