using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class AssignCommandTest
	{
		[TestMethod]
		[DataRow("roles", "assign")]
		[DataRow("roles", "add")]
		[DataRow("roles", "associate")]
		[DataRow("role", "assign")]
		[DataRow("role", "add")]
		[DataRow("role", "associate")]
		public void AliasesShouldParse(string noun, string verb)
		{
			var command = Utility.TestParseCommand<AssignCommand>("security", noun, verb, "--role", "Salesperson", "--user", "user@contoso.com");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("user@contoso.com", command.User);
			Assert.IsNull(command.Team);
			Assert.IsNull(command.BusinessUnit);
		}

		[TestMethod]
		[DataRow("--role", "--user", "--team", "--businessunit")]
		[DataRow("-r", "-u", "-t", "-bu")]
		public void AllOptionsShouldParse(string role, string user, string team, string businessUnit)
		{
			var command = Utility.TestParseCommand<AssignCommand>("security", "roles", "assign", role, "Salesperson", user, "user@contoso.com", team, "Sales", businessUnit, "Europe");
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
			var command = new AssignCommand { Role = "Salesperson", User = user, Team = team };
			Assert.AreEqual(expected, Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void RoleShouldBeRequired()
		{
			var command = new AssignCommand { User = "user" };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}