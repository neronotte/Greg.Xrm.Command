using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class RemoveHandlerCommandTest
	{
		[TestMethod]
		public void ParseWithLongNamesShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveHandlerCommand>(
				"forms", "removehandler",
				"--table", "account",
				"--library", "myprefix_scripts.js",
				"--function", "My.Account.onLoad");

			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual("myprefix_scripts.js", command.Library);
			Assert.AreEqual("My.Account.onLoad", command.Function);
			Assert.AreEqual(FormEvent.OnLoad, command.Event);
			Assert.IsFalse(command.Fast);
		}

		[TestMethod]
		public void ParseWithShortNamesShouldWork()
		{
			var command = Utility.TestParseCommand<RemoveHandlerCommand>(
				"forms", "removehandler",
				"-t", "account",
				"-l", "myprefix_scripts.js",
				"-fn", "My.Account.onNameChange",
				"-e", "OnChange",
				"-col", "name");

			Assert.AreEqual(FormEvent.OnChange, command.Event);
			Assert.AreEqual("name", command.Field);
		}

		[TestMethod]
		public void ValidateShouldFailWhenOnChangeHasNoField()
		{
			var command = new RemoveHandlerCommand
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
			var command = new RemoveHandlerCommand
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
		public void ValidateShouldPassForPlainOnLoad()
		{
			var command = new RemoveHandlerCommand
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
