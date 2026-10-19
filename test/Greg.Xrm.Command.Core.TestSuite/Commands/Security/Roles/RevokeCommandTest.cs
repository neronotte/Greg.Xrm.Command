using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class RevokeCommandTest
	{
		[TestMethod]
		[DataRow("roles", "revoke")]
		[DataRow("roles", "remove")]
		[DataRow("roles", "disassociate")]
		[DataRow("role", "revoke")]
		[DataRow("role", "remove")]
		[DataRow("role", "disassociate")]
		public void AliasesShouldParse(string noun, string verb)
		{
			var command = Utility.TestParseCommand<RevokeCommand>("security", noun, verb, "--role", "Salesperson", "--team", "Sales");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("Sales", command.Team);
			Assert.IsNull(command.User);
			Assert.IsNull(command.BusinessUnit);
		}

		[TestMethod]
		[DataRow("--role", "--user", "--team", "--businessunit")]
		[DataRow("-r", "-u", "-t", "-bu")]
		public void AllOptionsShouldParse(string role, string user, string team, string businessUnit)
		{
			var command = Utility.TestParseCommand<RevokeCommand>("security", "roles", "revoke", role, "Salesperson", user, "user@contoso.com", team, "Sales", businessUnit, "Europe");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("user@contoso.com", command.User);
			Assert.AreEqual("Sales", command.Team);
			Assert.AreEqual("Europe", command.BusinessUnit);
		}

		[TestMethod]
		[DataRow(null, null, false)]
		[DataRow(" ", " ", false)]
		[DataRow("user", null, true)]
		[DataRow(null, "team", true)]
		[DataRow("user", "team", true)]
		public void AtLeastOneRecipientShouldBeRequired(string? user, string? team, bool expected)
		{
			var command = new RevokeCommand { Role = "Salesperson", User = user, Team = team };
			Assert.AreEqual(expected, Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void RoleShouldBeRequired()
		{
			var command = new RevokeCommand { Team = "Sales" };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}