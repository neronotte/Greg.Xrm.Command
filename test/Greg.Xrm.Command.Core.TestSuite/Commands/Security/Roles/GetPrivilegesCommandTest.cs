using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class GetPrivilegesCommandTest
	{
		[TestMethod]
		[DataRow("roles", "get-privileges")]
		[DataRow("role", "get-privileges")]
		[DataRow("roles", "get")]
		[DataRow("role", "get")]
		[DataRow("roles", "getPrivileges")]
		[DataRow("role", "getPrivileges")]
		public void AllCommandFormsShouldParse(string roleVerb, string verb)
		{
			var command = Utility.TestParseCommand<GetPrivilegesCommand>("security", roleVerb, verb, "-r", "Salesperson");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual(RolePrivilegeViewMode.Assigned, command.Mode);
			Assert.AreEqual(RolePrivilegeOutputFormat.Functional, command.OutputFormat);
			Assert.IsNull(command.Table);
			Assert.IsNull(command.Privilege);
		}

		[TestMethod]
		[DataRow("--role", "--table", "--privilege", "--show", "--format")]
		[DataRow("-r", "-t", "-p", "-s", "-f")]
		public void AllOptionsShouldParse(string roleOption, string tableOption, string privilegeOption, string modeOption, string formatOption)
		{
			var command = Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get", roleOption, "Salesperson", tableOption, "claim", privilegeOption, "Read", modeOption, "All", formatOption, "c");
			Assert.AreEqual("Salesperson", command.Role);
			Assert.AreEqual("claim", command.Table);
			Assert.AreEqual("Read", command.Privilege);
			Assert.AreEqual(RolePrivilegeViewMode.All, command.Mode);
			Assert.AreEqual(RolePrivilegeOutputFormat.Compact, command.OutputFormat);
		}

		[TestMethod]
		[DataRow("c", RolePrivilegeOutputFormat.Compact)]
		[DataRow("compact", RolePrivilegeOutputFormat.Compact)]
		[DataRow("n", RolePrivilegeOutputFormat.Number)]
		[DataRow("number", RolePrivilegeOutputFormat.Number)]
		[DataRow("t", RolePrivilegeOutputFormat.Technical)]
		[DataRow("tech", RolePrivilegeOutputFormat.Technical)]
		[DataRow("technical", RolePrivilegeOutputFormat.Technical)]
		[DataRow("f", RolePrivilegeOutputFormat.Functional)]
		[DataRow("func", RolePrivilegeOutputFormat.Functional)]
		[DataRow("functional", RolePrivilegeOutputFormat.Functional)]
		[DataRow("C", RolePrivilegeOutputFormat.Compact)]
		[DataRow("N", RolePrivilegeOutputFormat.Number)]
		[DataRow("T", RolePrivilegeOutputFormat.Technical)]
		[DataRow("F", RolePrivilegeOutputFormat.Functional)]
		[DataRow("jsontech", RolePrivilegeOutputFormat.JsonTechnical)]
		[DataRow("jsontechnical", RolePrivilegeOutputFormat.JsonTechnical)]
		[DataRow("jt", RolePrivilegeOutputFormat.JsonTechnical)]
		[DataRow("json", RolePrivilegeOutputFormat.JsonFunctional)]
		[DataRow("jsonfunc", RolePrivilegeOutputFormat.JsonFunctional)]
		[DataRow("jsonfunctional", RolePrivilegeOutputFormat.JsonFunctional)]
		[DataRow("jf", RolePrivilegeOutputFormat.JsonFunctional)]
		[DataRow("jsonnumeric", RolePrivilegeOutputFormat.JsonNumeric)]
		[DataRow("jn", RolePrivilegeOutputFormat.JsonNumeric)]
		[DataRow("JT", RolePrivilegeOutputFormat.JsonTechnical)]
		[DataRow("JF", RolePrivilegeOutputFormat.JsonFunctional)]
		[DataRow("JN", RolePrivilegeOutputFormat.JsonNumeric)]
		public void FormatValuesShouldParseWithBothOptionNames(string format, RolePrivilegeOutputFormat expected)
		{
			foreach (var option in new[] { "--format", "-f" })
			{
				var command = Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get", "-r", "Salesperson", option, format);
				Assert.AreEqual(expected, command.OutputFormat);
				Assert.IsTrue(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
			}
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("bad")]
		[DataRow("1")]
		public void InvalidFormatShouldFailValidation(string? format)
		{
			var command = new GetPrivilegesCommand { Role = "Salesperson", Format = format };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		[DataRow("assigned", RolePrivilegeViewMode.Assigned)]
		[DataRow("ALL", RolePrivilegeViewMode.All)]
		[DataRow("Unassigned", RolePrivilegeViewMode.Unassigned)]
		public void ModeValuesShouldParse(string mode, RolePrivilegeViewMode expected)
		{
			foreach (var option in new[] { "--show", "-s" })
			{
				var command = Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get", "-r", "Salesperson", option, mode);
				Assert.AreEqual(expected, command.Mode);
			}
		}

		[TestMethod]
		public void ExplicitEmptyFormatShouldFailValidationWhileOmittedFormatUsesDefault()
		{
			var command = Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get", "-r", "Salesperson", "-f", "");
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void MissingRoleShouldBeRejected()
		{
			Assert.ThrowsExactly<CommandException>(() => Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get"));
		}

		[TestMethod]
		[DataRow("--name")]
		[DataRow("-n")]
		[DataRow("--mode")]
		[DataRow("--filter")]
		[DataRow("-m")]
		public void PreviousSelectorOptionsShouldBeRejected(string option)
		{
			Assert.ThrowsExactly<DataException>(() => Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get", "-r", "Salesperson", option, "Other value"));
		}

		[TestMethod]
		public void UsageExamplesShouldDescribeFiltersModesAndAllFormats()
		{
			using var text = new StringWriter();
			using var writer = new Services.MarkdownWriter(text);
			new GetPrivilegesCommand().WriteUsageExamples(writer);
			foreach (var value in new[] { "claim", "new_claimresponse", "NO assigned privilege", "c, compact", "n, number", "t, tech, technical", "f, func, functional", "CRWDATaS" })
				StringAssert.Contains(text.ToString(), value);
			StringAssert.Contains(text.ToString(), "-r \"Salesperson\"");
			StringAssert.Contains(text.ToString(), "--show");
			StringAssert.Contains(text.ToString(), "-s Unassigned");
			Assert.IsFalse(text.ToString().Contains("--filter"));
			StringAssert.Contains(text.ToString(), "jsonnumeric");
			Assert.IsFalse(text.ToString().Contains("-n \"Salesperson\""));
		}

		[TestMethod]
		public void UndefinedModeShouldFailValidation()
		{
			var command = new GetPrivilegesCommand { Role = "Salesperson", Mode = (RolePrivilegeViewMode)99 };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}

		[TestMethod]
		public void ObsoleteCompactFlagShouldBeRejected()
		{
			Assert.ThrowsExactly<DataException>(() => Utility.TestParseCommand<GetPrivilegesCommand>("security", "roles", "get", "-r", "Salesperson", "--compact"));
		}
	}
}