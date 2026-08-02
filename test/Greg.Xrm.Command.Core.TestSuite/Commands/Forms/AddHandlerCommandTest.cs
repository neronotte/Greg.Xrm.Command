using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class AddHandlerCommandTest
	{
		// ── parsing ───────────────────────────────────────────────────────────

		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<AddHandlerCommand>(
				"forms", "addhandler",
				"--table", "account",
				"--library", "myprefix_scripts.js",
				"--function", "My.Account.onLoad");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("myprefix_scripts.js", command.Library);
			Assert.AreEqual("My.Account.onLoad", command.Function);
			Assert.AreEqual(FormEvent.OnLoad, command.Event);
			Assert.IsTrue(command.PassExecutionContext);
			Assert.IsNull(command.Field);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<AddHandlerCommand>(
				"forms", "addhandler",
				"-t", "account",
				"-l", "myprefix_scripts.js",
				"-fn", "My.Account.onNameChange",
				"-e", "OnChange",
				"-col", "name");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual(FormEvent.OnChange, command.Event);
			Assert.AreEqual("name", command.Field);
		}

		[TestMethod]
		public void FastShouldDefaultToFalseAndBeSettable()
		{
			var command = Utility.TestParseCommand<AddHandlerCommand>(
				"forms", "addhandler",
				"-t", "account",
				"-l", "myprefix_scripts.js",
				"-fn", "My.Account.onLoad");

			Assert.IsFalse(command.Fast);

			command = Utility.TestParseCommand<AddHandlerCommand>(
				"forms", "addhandler",
				"-t", "account",
				"-l", "myprefix_scripts.js",
				"-fn", "My.Account.onLoad",
				"--fast", "true");

			Assert.IsTrue(command.Fast);
		}

		[TestMethod]
		public void OutputOptionShouldWork()
		{
			var command = Utility.TestParseCommand<AddHandlerCommand>(
				"forms", "addhandler",
				"-t", "account",
				"-l", "myprefix_scripts.js",
				"-fn", "My.Account.onLoad",
				"--output", "C:\\temp");

			Assert.AreEqual("C:\\temp", command.TempDir);
		}

		[TestMethod]
		public void PassContextCanBeDisabled()
		{
			var command = Utility.TestParseCommand<AddHandlerCommand>(
				"forms", "addhandler",
				"-t", "account",
				"-l", "myprefix_scripts.js",
				"-fn", "My.Account.onLoad",
				"--passContext", "false");

			Assert.IsFalse(command.PassExecutionContext);
		}

		// ── validation ────────────────────────────────────────────────────────

		[TestMethod]
		public void ValidateShouldFailWhenOnChangeHasNoField()
		{
			var command = new AddHandlerCommand
			{
				TableName = "account",
				Library = "myprefix_scripts.js",
				Function = "My.Account.onNameChange",
				Event = FormEvent.OnChange
			};

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void ValidateShouldFailWhenFieldIsUsedWithoutOnChange()
		{
			var command = new AddHandlerCommand
			{
				TableName = "account",
				Library = "myprefix_scripts.js",
				Function = "My.Account.onLoad",
				Event = FormEvent.OnLoad,
				Field = "name"
			};

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void ValidateShouldPassForOnChangeWithField()
		{
			var command = new AddHandlerCommand
			{
				TableName = "account",
				Library = "myprefix_scripts.js",
				Function = "My.Account.onNameChange",
				Event = FormEvent.OnChange,
				Field = "name"
			};

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(0, results.Count);
		}

		[TestMethod]
		public void ValidateShouldPassForPlainOnLoad()
		{
			var command = new AddHandlerCommand
			{
				TableName = "account",
				Library = "myprefix_scripts.js",
				Function = "My.Account.onLoad"
			};

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(0, results.Count);
		}
	}
}
