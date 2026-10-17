using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class CloneCommandTest
	{
		[TestMethod]
		[DataRow("roles", "clone")]
		[DataRow("roles", "copy")]
		[DataRow("role", "clone")]
		[DataRow("role", "copy")]
		public void AliasesShouldParseWithAndWithoutSecurityPrefix(string noun, string verb)
		{
			var command = Utility.TestParseCommand<CloneCommand>(noun, verb, "--role", "Salesperson");
			Assert.AreEqual("Salesperson", command.Role);
			var securityCommand = Utility.TestParseCommand<CloneCommand>("security", noun, verb, "-r", "Salesperson");
			Assert.AreEqual(command.Role, securityCommand.Role);
		}

		[TestMethod]
		[DataRow("--role", "--name", "--description", "--businessunit", "--inheritance")]
		[DataRow("-r", "-n", "-d", "-bu", "-i")]
		public void AllOptionsShouldParse(string role, string name, string description, string businessUnit, string inheritance)
		{
			var command = Utility.TestParseCommand<CloneCommand>("roles", "clone", role, "Salesperson", name, "Regional Sales",
				description, "Description", businessUnit, "Europe", inheritance, "TeamOnly");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("Regional Sales", command.Name);
			Assert.AreEqual("Description", command.Description);
			Assert.AreEqual("Europe", command.BusinessUnit);
			Assert.AreEqual(MemberPrivilegeInheritance.TeamOnly, command.Inheritance);
		}

		[TestMethod]
		public void OmittedOverridesShouldRemainNull()
		{
			var command = Utility.TestParseCommand<CloneCommand>("roles", "clone", "-r", "Salesperson");
			Assert.IsNull(command.Name);
			Assert.IsNull(command.Description);
			Assert.IsNull(command.BusinessUnit);
			Assert.IsNull(command.Inheritance);
		}

		[TestMethod]
		[DataRow("0", MemberPrivilegeInheritance.TeamOnly)]
		[DataRow("1", MemberPrivilegeInheritance.DirectUserAndTeam)]
		[DataRow("TeamOnly", MemberPrivilegeInheritance.TeamOnly)]
		[DataRow("DirectUserAndTeam", MemberPrivilegeInheritance.DirectUserAndTeam)]
		public void InheritanceShouldParse(string value, MemberPrivilegeInheritance expected)
		{
			var command = Utility.TestParseCommand<CloneCommand>("roles", "clone", "-r", "Salesperson", "-i", value);
			Assert.AreEqual(expected, command.Inheritance);
		}

		[TestMethod]
		public void RoleShouldBeRequired()
		{
			var command = new CloneCommand();
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void UndefinedInheritanceShouldFailValidation()
		{
			var command = new CloneCommand { Role = "Salesperson", Inheritance = (MemberPrivilegeInheritance)2 };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void OversizedNameOrDescriptionShouldFailValidation()
		{
			var command = new CloneCommand { Role = "Salesperson", Name = new string('x', 101) };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
			command.Name = null;
			command.Description = new string('x', 2001);
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}