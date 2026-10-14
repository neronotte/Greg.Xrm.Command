using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class ClearPrivilegeCommandTest
	{
		[TestMethod]
		[DataRow("roles", "--role", "--name")]
		[DataRow("role", "-r", "-n")]
		public void TechnicalNameShouldParse(string verb, string roleOption, string nameOption)
		{
			var command = Utility.TestParseCommand<ClearPrivilegeCommand>("security", verb, "clear-privilege", roleOption, "Salesperson", nameOption, "prvWriteAccount");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("prvWriteAccount", command.Name);
			Assert.IsNull(command.Table);
			Assert.IsNull(command.Privilege);
			Assert.IsTrue(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		[DataRow("--table", "--privilege")]
		[DataRow("-t", "-p")]
		public void TableAndPrivilegeShouldParse(string tableOption, string privilegeOption)
		{
			var command = Utility.TestParseCommand<ClearPrivilegeCommand>("security", "roles", "clear-privilege", "-r", "Salesperson", tableOption, "Account", privilegeOption, "Write");
			Assert.AreEqual("Account", command.Table);
			Assert.AreEqual("Write", command.Privilege);
			Assert.IsNull(command.Name);
			Assert.IsTrue(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		[DataRow(null, null, null)]
		[DataRow(null, "Account", null)]
		[DataRow(null, null, "Write")]
		[DataRow("prvWriteAccount", "Account", null)]
		[DataRow("prvWriteAccount", null, "Write")]
		public void InvalidPrivilegeSelectorsShouldFailValidation(string? name, string? table, string? privilege)
		{
			var command = new ClearPrivilegeCommand { Role = "Salesperson", Name = name, Table = table, Privilege = privilege };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void MissingRoleShouldFailValidation()
		{
			var command = new ClearPrivilegeCommand { Name = "prvWriteAccount" };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void LevelOptionShouldBeRejected()
		{
			Assert.ThrowsExactly<DataException>(() => Utility.TestParseCommand<ClearPrivilegeCommand>("security", "roles", "clear-privilege", "-r", "Salesperson", "-n", "prvWriteAccount", "-l", "Global"));
		}

		[TestMethod]
		[DataRow(typeof(SetPrivilegeCommand))]
		[DataRow(typeof(ClearPrivilegeCommand))]
		public void RolesShouldBePrimaryNamespaceAndRoleShouldBeAlias(Type commandType)
		{
			Assert.AreEqual("roles", commandType.GetCustomAttribute<CommandAttribute>()!.Verbs[1]);
			Assert.IsTrue(commandType.GetCustomAttributes<AliasAttribute>().Any(alias =>
				alias.Verbs.SequenceEqual(["security", "role", commandType.GetCustomAttribute<CommandAttribute>()!.Verbs[2]])));
		}
	}
}