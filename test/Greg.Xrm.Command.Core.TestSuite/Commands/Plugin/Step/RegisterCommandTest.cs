using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Services.Plugin;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	[TestClass]
	public class RegisterCommandTest
	{
		// ── --preImage / --preim ───────────────────────────────────────────────

		[TestMethod]
		public void PreImageDefaultShouldBeFalse()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Create");

			Assert.IsFalse(command.PreImage);
			Assert.IsFalse(command.EffectivePreImage);
		}

		[TestMethod]
		public void PreImageLongFlagShouldWork()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Update",
				"--table", "account",
				"--preImage");

			Assert.IsTrue(command.PreImage);
			Assert.IsTrue(command.EffectivePreImage);
		}

		[TestMethod]
		public void PreImageShortFlagShouldWork()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Update",
				"--table", "account",
				"-preim");

			Assert.IsTrue(command.PreImage);
			Assert.IsTrue(command.EffectivePreImage);
		}

		// ── --postImage / --postim ────────────────────────────────────────────

		[TestMethod]
		public void PostImageDefaultShouldBeFalse()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Create");

			Assert.IsFalse(command.PostImage);
			Assert.IsFalse(command.EffectivePostImage);
		}

		[TestMethod]
		public void PostImageLongFlagShouldWork()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Update",
				"--table", "account",
				"--postImage");

			Assert.IsTrue(command.PostImage);
			Assert.IsTrue(command.EffectivePostImage);
		}

		// ── --preImageAttributes / --preimat ──────────────────────────────────

		[TestMethod]
		public void PreImageAttributesDefaultShouldBeNull()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Create");

			Assert.IsNull(command.PreImageAttributes);
		}

		[TestMethod]
		public void PreImageAttributesLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Update",
				"--table", "account",
				"--preImageAttributes", "name,accountnumber");

			Assert.AreEqual("name,accountnumber", command.PreImageAttributes);
		}

		[TestMethod]
		public void PreImageAttributesShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Update",
				"--table", "account",
				"-preimat", "name,telephone1");

			Assert.AreEqual("name,telephone1", command.PreImageAttributes);
		}

		[TestMethod]
		public void PreImageAttributesImpliesEffectivePreImage()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PreImageAttributes = "name,accountnumber"
			};

			Assert.IsFalse(command.PreImage);
			Assert.IsTrue(command.EffectivePreImage);
		}

		// ── --postImageAttributes / --postimat ────────────────────────────────

		[TestMethod]
		public void PostImageAttributesDefaultShouldBeNull()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Create");

			Assert.IsNull(command.PostImageAttributes);
		}

		[TestMethod]
		public void PostImageAttributesLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<RegisterCommand>(
				"plugin", "step", "register",
				"--class", "MyPlugin",
				"--message", "Update",
				"--table", "account",
				"--postImageAttributes", "name,revenue");

			Assert.AreEqual("name,revenue", command.PostImageAttributes);
		}

		[TestMethod]
		public void PostImageAttributesImpliesEffectivePostImage()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PostImageAttributes = "name,revenue"
			};

			Assert.IsFalse(command.PostImage);
			Assert.IsTrue(command.EffectivePostImage);
		}

		// ── Validation: image requires PrimaryEntityName ───────────────────────

		[TestMethod]
		public void ValidateShouldFailWhenPreImageAttributesSetWithoutTable()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Create",
				PreImageAttributes = "name"
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PreImage))));
		}

		[TestMethod]
		public void ValidateShouldFailWhenPostImageAttributesSetWithoutTable()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Create",
				PostImageAttributes = "name"
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PostImage))));
		}

		[TestMethod]
		public void ValidateShouldFailWhenPreImageSetWithoutTable()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Create",
				PreImage = true
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PreImage))));
		}

		// ── Validation: PreImageName requires EffectivePreImage ───────────────

		[TestMethod]
		public void ValidateShouldFailWhenPreImageNameSetWithoutPreImage()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PreImageName = "my_pre"
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PreImageName))));
		}

		[TestMethod]
		public void ValidateShouldPassWhenPreImageNameSetWithPreImageAttributes()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PreImageName = "my_pre",
				PreImageAttributes = "name"
			};

			var results = Validate(command);
			Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PreImageName))));
		}

		[TestMethod]
		public void ValidateShouldFailWhenPostImageNameSetWithoutPostImage()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PostImageName = "my_post"
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PostImageName))));
		}

		[TestMethod]
		public void ValidateShouldPassWhenPostImageNameSetWithPostImageAttributes()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PostImageName = "my_post",
				PostImageAttributes = "name"
			};

			var results = Validate(command);
			Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PostImageName))));
		}

		// ── Validation: attribute name format ─────────────────────────────────

		[TestMethod]
		[DataRow("name")]
		[DataRow("accountnumber")]
		[DataRow("telephone1")]
		[DataRow("new_customfield")]
		[DataRow("name,accountnumber,telephone1")]
		[DataRow("name, accountnumber")]
		public void ValidateShouldPassForValidAttributeNames(string attributes)
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PreImageAttributes = attributes
			};

			var results = Validate(command);
			Assert.IsFalse(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PreImageAttributes))),
				$"Expected no errors for attributes: '{attributes}'");
		}

		[TestMethod]
		[DataRow("Name")]
		[DataRow("ACCOUNTNUMBER")]
		[DataRow("123field")]
		[DataRow("field-name")]
		[DataRow("field name")]
		public void ValidateShouldFailForInvalidAttributeNames(string attributes)
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PreImageAttributes = attributes
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PreImageAttributes))),
				$"Expected error for invalid attribute: '{attributes}'");
		}

		[TestMethod]
		[DataRow("Name")]
		[DataRow("REVENUE")]
		[DataRow("1amount")]
		public void ValidateShouldFailForInvalidPostImageAttributeNames(string attributes)
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PostImageAttributes = attributes
			};

			var results = Validate(command);
			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(RegisterCommand.PostImageAttributes))),
				$"Expected error for invalid post-image attribute: '{attributes}'");
		}

		// ── Validation: EffectivePreImage / EffectivePostImage symmetry ────────

		[TestMethod]
		public void EffectivePreImageFalseWhenNeitherFlagNorAttributes()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account"
			};

			Assert.IsFalse(command.EffectivePreImage);
		}

		[TestMethod]
		public void EffectivePostImageFalseWhenNeitherFlagNorAttributes()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account"
			};

			Assert.IsFalse(command.EffectivePostImage);
		}

		[TestMethod]
		public void EffectivePreImageTrueWhenFlagSet()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PreImage = true
			};

			Assert.IsTrue(command.EffectivePreImage);
		}

		[TestMethod]
		public void EffectivePostImageTrueWhenFlagSet()
		{
			var command = new RegisterCommand
			{
				PluginTypeName = "MyPlugin",
				MessageName = "Update",
				PrimaryEntityName = "account",
				PostImage = true
			};

			Assert.IsTrue(command.EffectivePostImage);
		}

		// ── Helpers ──────────────────────────────────────────────────────────

		private static IReadOnlyList<ValidationResult> Validate(RegisterCommand command)
		{
			var ctx = new ValidationContext(command);
			var results = new List<ValidationResult>();
			Validator.TryValidateObject(command, ctx, results, validateAllProperties: true);
			return results;
		}
	}
}
