using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	[TestClass]
	public class GetRolesCommandTest
	{
		[TestMethod]
		[DataRow("teams")]
		[DataRow("team")]
		public void AllAliasesShouldParse(string noun)
		{
			Assert.AreEqual("Sales", Utility.TestParseCommand<GetRolesCommand>("security", noun, "get", "roles", "--team", "Sales").Team);
			Assert.AreEqual("Sales", Utility.TestParseCommand<GetRolesCommand>("security", noun, "get-roles", "-t", "Sales").Team);
			Assert.AreEqual("Sales", Utility.TestParseCommand<GetRolesCommand>("security", noun, "getRoles", "-t", "Sales").Team);
		}

		[TestMethod]
		public void TeamShouldBeRequired()
		{
			var command = new GetRolesCommand();
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}