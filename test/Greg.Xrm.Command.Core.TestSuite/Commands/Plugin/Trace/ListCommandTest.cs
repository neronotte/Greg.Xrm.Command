using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin.Trace
{
	[TestClass]
	public class ListCommandTest
	{
		// ── no options — valid because everything is optional ─────────────────

		[TestMethod]
		public void ParseWithNoOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list");

			Assert.IsNull(command.TypeName);
			Assert.AreEqual(10, command.Top);
			Assert.IsFalse(command.ErrorsOnly);
		}

		// ── --name / -n ───────────────────────────────────────────────────────

		[TestMethod]
		public void NameOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"--name", "MyPlugin");

			Assert.AreEqual("MyPlugin", command.TypeName);
		}

		[TestMethod]
		public void NameOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"-n", "MyPlugin.Plugin1");

			Assert.AreEqual("MyPlugin.Plugin1", command.TypeName);
		}

		// ── --top / -t ────────────────────────────────────────────────────────

		[TestMethod]
		public void TopShouldDefaultTo10()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list");

			Assert.AreEqual(10, command.Top);
		}

		[TestMethod]
		public void TopOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"--top", "25");

			Assert.AreEqual(25, command.Top);
		}

		[TestMethod]
		public void TopOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"-t", "5");

			Assert.AreEqual(5, command.Top);
		}

		// ── --errors-only / -e ────────────────────────────────────────────────

		[TestMethod]
		public void ErrorsOnlyShouldDefaultToFalse()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list");

			Assert.IsFalse(command.ErrorsOnly);
		}

		[TestMethod]
		public void ErrorsOnlyWithLongNameShouldBeTrueWhenProvided()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"--errors-only");

			Assert.IsTrue(command.ErrorsOnly);
		}

		[TestMethod]
		public void ErrorsOnlyWithShortNameShouldBeTrueWhenProvided()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"-e");

			Assert.IsTrue(command.ErrorsOnly);
		}

		// ── combined ──────────────────────────────────────────────────────────

		[TestMethod]
		public void AllOptionsTogetherShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>(
				"plugin", "trace", "list",
				"-n", "MyPlugin",
				"-t", "5",
				"-e");

			Assert.AreEqual("MyPlugin", command.TypeName);
			Assert.AreEqual(5, command.Top);
			Assert.IsTrue(command.ErrorsOnly);
		}

		// ── validation ────────────────────────────────────────────────────────

		[TestMethod]
		public void ValidateShouldFailWhenTopIsZeroOrNegative()
		{
			var command = new ListCommand { Top = 0 };

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void ValidateShouldPassForDefaultValues()
		{
			var command = new ListCommand();

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(0, results.Count);
		}
	}
}
