using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin.Type
{
	[TestClass]
	public class UnregisterCommandTest
	{
		// ── --id parsing ───────────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithIdLongNameShouldWork()
		{
			var id = Guid.NewGuid();
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--id", id.ToString());

			Assert.AreEqual(id, command.TypeId);
		}

		// ── --name parsing ─────────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithNameLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--name", "MyPlugin");

			Assert.AreEqual("MyPlugin", command.PluginTypeName);
		}

		[TestMethod]
		public void ParseWithNameShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"-n", "MyPlugin");

			Assert.AreEqual("MyPlugin", command.PluginTypeName);
		}

		// ── --force parsing ────────────────────────────────────────────────────

		[TestMethod]
		public void ForceFlagShouldDefaultToFalse()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--name", "MyPlugin");

			Assert.IsFalse(command.Force);
		}

		[TestMethod]
		public void ForceFlagShouldBeParsedWithLongNameWhenProvided()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--name", "MyPlugin",
				"--force");

			Assert.IsTrue(command.Force);
		}

		[TestMethod]
		public void ForceFlagShouldBeParsedWithShortNameWhenProvided()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--name", "MyPlugin",
				"-f");

			Assert.IsTrue(command.Force);
		}

		// ── Default values ─────────────────────────────────────────────────────

		[TestMethod]
		public void DefaultValuesShouldBeCorrectWhenOnlyIdIsProvided()
		{
			var id = Guid.NewGuid();
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--id", id.ToString());

			Assert.AreEqual(id, command.TypeId);
			Assert.IsNull(command.PluginTypeName);
			Assert.IsFalse(command.Force);
		}

		[TestMethod]
		public void DefaultValuesShouldBeCorrectWhenOnlyNameIsProvided()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "unregister",
				"--name", "MyPlugin");

			Assert.IsNull(command.TypeId);
			Assert.AreEqual("MyPlugin", command.PluginTypeName);
			Assert.IsFalse(command.Force);
		}

		// ── Aliases ────────────────────────────────────────────────────────────

		[TestMethod]
		public void AliasPluTypeRemoveShouldWork()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "remove",
				"--name", "MyPlugin");

			Assert.AreEqual("MyPlugin", command.PluginTypeName);
		}

		[TestMethod]
		public void AliasPluginTypeDelShouldWork()
		{
			var command = Utility.TestParseCommand<UnregisterCommand>(
				"plugin", "type", "del",
				"--name", "MyPlugin");

			Assert.AreEqual("MyPlugin", command.PluginTypeName);
		}

		// ── Validation ─────────────────────────────────────────────────────────

		[TestMethod]
		public void Validate_ShouldFail_WhenNeitherIdNorNameIsProvided()
		{
			var command = new UnregisterCommand();
			var context = new ValidationContext(command);

			var results = command.Validate(context).ToList();

			Assert.AreEqual(1, results.Count);
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(UnregisterCommand.TypeId));
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(UnregisterCommand.PluginTypeName));
		}

		[TestMethod]
		public void Validate_ShouldFail_WhenBothIdAndNameAreProvided()
		{
			var command = new UnregisterCommand
			{
				TypeId = Guid.NewGuid(),
				PluginTypeName = "MyPlugin"
			};
			var context = new ValidationContext(command);

			var results = command.Validate(context).ToList();

			Assert.AreEqual(1, results.Count);
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(UnregisterCommand.TypeId));
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(UnregisterCommand.PluginTypeName));
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenOnlyIdIsProvided()
		{
			var command = new UnregisterCommand { TypeId = Guid.NewGuid() };
			var context = new ValidationContext(command);

			var results = command.Validate(context).ToList();

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void Validate_ShouldPass_WhenOnlyNameIsProvided()
		{
			var command = new UnregisterCommand { PluginTypeName = "MyPlugin" };
			var context = new ValidationContext(command);

			var results = command.Validate(context).ToList();

			Assert.AreEqual(0, results.Count);
		}
	}
}
