namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ReplicateCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<ReplicateCommand>(
				"view", "replicate",
				"--name", "My View",
				"--table", "account",
				"--keepComponents", "true",
				"--keepSorting", "true");

			Assert.AreEqual("My View", command.ViewName);
			Assert.AreEqual("account", command.TableName);
			Assert.IsTrue(command.KeepComponents);
			Assert.IsTrue(command.KeepSorting);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<ReplicateCommand>(
				"view", "replicate",
				"-n", "My View",
				"-kc", "true",
				"-ks", "true");

			Assert.IsTrue(command.KeepComponents);
			Assert.IsTrue(command.KeepSorting);
		}

		[TestMethod]
		public void ParseShouldReplicateEverythingByDefault()
		{
			var command = Utility.TestParseCommand<ReplicateCommand>(
				"view", "replicate",
				"--name", "My View");

			Assert.IsFalse(command.KeepComponents);
			Assert.IsFalse(command.KeepSorting);
		}
	}
}
