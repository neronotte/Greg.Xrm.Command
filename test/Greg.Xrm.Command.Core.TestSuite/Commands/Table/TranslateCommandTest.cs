using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class TranslateCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"table", "translate",
				"--table", "account",
				"--singular", "1033:\"Account\"",
				"--plural", "1033:\"Accounts\"");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("1033:\"Account\"", command.Singular);
			Assert.AreEqual("1033:\"Accounts\"", command.Plural);
			Assert.IsFalse(command.NoPublish);
		}

		[TestMethod]
		public void ParseDescriptionShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"table", "translate",
				"--table", "account",
				"--singular", "1033:\"Account\"",
				"--plural", "1033:\"Accounts\"",
				"--description", "1033:\"Customer account\"");

			Assert.AreEqual("1033:\"Customer account\"", command.Description);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"table", "translate",
				"-t", "account",
				"-s", "1033:\"Account\"",
				"-p", "1033:\"Accounts\"",
				"-np");

			Assert.IsTrue(command.NoPublish);
		}

		[TestMethod]
		public void Validate_ShouldRejectDifferentLanguageSets()
		{
			var command = new TranslateCommand
			{
				Singular = "1033:\"Account\"",
				Plural = "1040:\"Aziende\"",
			};

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.HasCount(1, results);
		}
	}
}
