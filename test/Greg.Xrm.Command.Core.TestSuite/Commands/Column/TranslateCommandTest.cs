namespace Greg.Xrm.Command.Commands.Column
{
	[TestClass]
	public class TranslateCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"column",
				"translate",
				"--table", "account",
				"--column", "new_name",
				"--labels", "1033:\"Customer\"");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("new_name", command.ColumnName);
			Assert.AreEqual("1033:\"Customer\"", command.Labels);
			Assert.IsFalse(command.NoPublish);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"column",
				"translate",
				"-t", "account",
				"-c", "new_name",
				"-l", "1033:\"Customer\"",
				"-np");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("new_name", command.ColumnName);
			Assert.IsTrue(command.NoPublish);
		}

		[TestMethod]
		public void ParseDescriptionAndBooleanLabelsShouldWork()
		{
			var command = Utility.TestParseCommand<TranslateCommand>(
				"column", "translate",
				"-t", "account",
				"-c", "new_ispreferred",
				"-d", "1033:\"Preferred customer\"",
				"-tl", "1033:\"Yes\"",
				"-fl", "1033:\"No\"");

			Assert.AreEqual("1033:\"Preferred customer\"", command.Description);
			Assert.AreEqual("1033:\"Yes\"", command.TrueLabel);
			Assert.AreEqual("1033:\"No\"", command.FalseLabel);
		}
	}
}
