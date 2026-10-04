using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[TestClass]
	public class GetRolesCommandTest
	{
		[TestMethod]
		[DataRow("users", "getRoles")]
		[DataRow("user", "getRoles")]
		[DataRow("users", "get-roles")]
		[DataRow("user", "get-roles")]
		[DataRow("users", "getRole")]
		[DataRow("user", "getRole")]
		[DataRow("users", "get-role")]
		[DataRow("user", "get-role")]
		public void AllAliasesShouldParseWithAndWithoutSecurityPrefix(string noun, string verb)
		{
			var command = Utility.TestParseCommand<GetRolesCommand>(noun, verb, "--user", "john.doe@contoso.com");
			Assert.AreEqual("john.doe@contoso.com", command.User);
			var securityCommand = Utility.TestParseCommand<GetRolesCommand>("security", noun, verb, "--user", "john.doe@contoso.com");
			Assert.AreEqual(command.User, securityCommand.User);
		}

		[TestMethod]
		public void UserRolesAliasShouldParseWithOptionalUser()
		{
			var currentUser = Utility.TestParseCommand<GetRolesCommand>("user", "roles");
			Assert.IsNull(currentUser.User);
			var selectedUser = Utility.TestParseCommand<GetRolesCommand>("user", "roles", "-u", "john.doe@contoso.com");
			Assert.AreEqual("john.doe@contoso.com", selectedUser.User);
		}

		[TestMethod]
		[DataRow("--user")]
		[DataRow("-u")]
		public void UserOptionShouldParse(string option)
		{
			var identifier = Guid.NewGuid().ToString();
			var command = Utility.TestParseCommand<GetRolesCommand>("users", "getRoles", option, identifier);
			Assert.AreEqual(identifier, command.User);
		}

		[TestMethod]
		public void OmittedUserShouldRemainNull()
		{
			var command = Utility.TestParseCommand<GetRolesCommand>("users", "getRoles");
			Assert.IsNull(command.User);
		}

		[TestMethod]
		public void UsageExamplesShouldExplainDelegationAndDefaultUser()
		{
			using var text = new StringWriter();
			using var writer = new MarkdownWriter(text);
			new GetRolesCommand().WriteUsageExamples(writer);
			var markdown = text.ToString();
			StringAssert.Contains(markdown, "security roles get-by-user");
			StringAssert.Contains(markdown, "current connected user");
			StringAssert.Contains(markdown, "inherited through team membership");
			StringAssert.Contains(markdown, "pacx users getRoles");
			StringAssert.Contains(markdown, "--user");
			StringAssert.Contains(markdown, "-u");
		}
	}
}