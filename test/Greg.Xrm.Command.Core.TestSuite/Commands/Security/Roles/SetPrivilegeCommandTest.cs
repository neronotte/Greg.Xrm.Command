using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class SetPrivilegeCommandTest
	{
		[TestMethod]
		public void UsageExamplesShouldDescribeAllLevelsAndRestrictions()
		{
			using var text = new StringWriter();
			using var writer = new MarkdownWriter(text);
			new SetPrivilegeCommand().WriteUsageExamples(writer);
			var markdown = text.ToString();

			foreach (var number in new[] { "0", "1", "2", "3", "4" })
				StringAssert.Contains(markdown, "| " + number);
			foreach (var name in new[] { "Basic, User", "Local, BusinessUnit", "Deep, ParentChild", "Global, Organization", "null or \"\"" })
				StringAssert.Contains(markdown, name);
			StringAssert.Contains(markdown, "case-insensitive");
			StringAssert.Contains(markdown, "specific privilege");
			StringAssert.Contains(markdown, "omitting --level is an error");
			StringAssert.Contains(markdown, "Only unmanaged security roles");
			StringAssert.Contains(markdown, "-l 2");
			StringAssert.Contains(markdown, "-l 0");
		}

		[TestMethod]
		[DataRow("--role", "--name", "--level")]
		[DataRow("-r", "-n", "-l")]
		public void TechnicalNameShouldParse(string roleOption, string nameOption, string levelOption)
		{
			var command = Utility.TestParseCommand<SetPrivilegeCommand>("security", "role", "set-privilege", roleOption, "Salesperson", nameOption, "prvWriteAccount", levelOption, "Basic");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("prvWriteAccount", command.Name);
			Assert.AreEqual("Basic", command.Level);
			Assert.IsTrue(command.LevelSpecified);
			Assert.IsNull(command.Table);
			Assert.IsNull(command.Privilege);
		}

		[TestMethod]
		[DataRow("--table", "--privilege")]
		[DataRow("-t", "-p")]
		public void TableAndPrivilegeShouldParse(string tableOption, string privilegeOption)
		{
			var command = Utility.TestParseCommand<SetPrivilegeCommand>("security", "roles", "set-privilege", "-r", "Salesperson", tableOption, "Account", privilegeOption, "Write", "-l", "Global");
			Assert.AreEqual("Account", command.Table);
			Assert.AreEqual("Write", command.Privilege);
			Assert.IsNull(command.Name);
		}

		[TestMethod]
		[DataRow("")]
		[DataRow("null")]
		[DataRow("0")]
		public void RemovalLevelShouldParse(string level)
		{
			var command = Utility.TestParseCommand<SetPrivilegeCommand>("security", "role", "set-privilege", "-r", "Salesperson", "-n", "prvWriteAccount", "-l", level);
			Assert.IsTrue(command.LevelSpecified);
			Assert.IsTrue(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		[DataRow("1")]
		[DataRow("2")]
		[DataRow("3")]
		[DataRow("4")]
		public void NumericLevelShouldParse(string level)
		{
			var command = Utility.TestParseCommand<SetPrivilegeCommand>("security", "roles", "set-privilege", "-r", "Salesperson", "-n", "prvWriteAccount", "-l", level);
			Assert.AreEqual(level, command.Level);
			Assert.IsTrue(command.LevelSpecified);
		}

		[TestMethod]
		public void OmittedLevelShouldFailValidation()
		{
			var command = new SetPrivilegeCommand { Role = "Salesperson", Name = "prvWriteAccount" };
			Assert.IsFalse(command.LevelSpecified);
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		[DataRow(null, null, null)]
		[DataRow(null, "Account", null)]
		[DataRow(null, null, "Write")]
		[DataRow("prvWriteAccount", "Account", null)]
		[DataRow("prvWriteAccount", null, "Write")]
		public void InvalidPrivilegeSelectorsShouldFailValidation(string? name, string? table, string? privilege)
		{
			var command = new SetPrivilegeCommand { Role = "Salesperson", Name = name, Table = table, Privilege = privilege, Level = "Basic" };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}