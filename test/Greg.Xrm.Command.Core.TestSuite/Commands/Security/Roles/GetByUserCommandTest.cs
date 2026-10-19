namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class GetByUserCommandTest
	{
		[TestMethod]
		public void ParseWithoutUserShouldWork()
		{
			var command = Utility.TestParseCommand<GetByUserCommand>("security", "roles", "get-by-user");

			Assert.IsNull(command.User);
		}

		[TestMethod]
		public void ParseWithUserIdShouldWork()
		{
			var userId = Guid.NewGuid().ToString();
			var command = Utility.TestParseCommand<GetByUserCommand>("security", "roles", "get-by-user", "--user", userId);

			Assert.AreEqual(userId, command.User);
		}

		[TestMethod]
		public void ParseWithEmailAndShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<GetByUserCommand>("security", "roles", "get-by-user", "-u", "john.doe@contoso.com");

			Assert.AreEqual("john.doe@contoso.com", command.User);
		}
	}
}
