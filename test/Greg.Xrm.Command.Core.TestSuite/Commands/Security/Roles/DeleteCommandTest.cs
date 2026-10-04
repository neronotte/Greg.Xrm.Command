using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class DeleteCommandTest
	{
		[TestMethod]
		[DataRow("roles")]
		[DataRow("role")]
		public void AliasesShouldParseWithAndWithoutSecurityPrefix(string noun)
		{
			var command = Utility.TestParseCommand<DeleteCommand>(noun, "delete", "--role", "Salesperson - Copy");
			Assert.AreEqual("Salesperson - Copy", command.Role);
			var securityCommand = Utility.TestParseCommand<DeleteCommand>("security", noun, "delete", "-r", "Salesperson - Copy");
			Assert.AreEqual(command.Role, securityCommand.Role);
		}

		[TestMethod]
		[DataRow("--role")]
		[DataRow("-r")]
		public void RoleGuidShouldParse(string option)
		{
			var roleId = Guid.NewGuid().ToString();
			var command = Utility.TestParseCommand<DeleteCommand>("roles", "delete", option, roleId);
			Assert.AreEqual(roleId, command.Role);
		}

		[TestMethod]
		[DataRow("")]
		[DataRow(" ")]
		public void MissingOrWhitespaceRoleShouldFailValidation(string role)
		{
			var command = new DeleteCommand { Role = role };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void UsageShouldDescribeManagedGuardAndDeletionRisks()
		{
			using var text = new StringWriter();
			using var writer = new MarkdownWriter(text);
			new DeleteCommand().WriteUsageExamples(writer);
			StringAssert.Contains(text.ToString(), "Only unmanaged roles");
			StringAssert.Contains(text.ToString(), "without a confirmation prompt");
			StringAssert.Contains(text.ToString(), "users and teams");
		}
	}
}