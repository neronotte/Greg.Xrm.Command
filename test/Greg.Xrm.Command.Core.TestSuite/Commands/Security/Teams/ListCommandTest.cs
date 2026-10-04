using Greg.Xrm.Command.Model;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	[TestClass]
	public class ListCommandTest
	{
		[TestMethod]
		[DataRow("teams", "--type", "--name")]
		[DataRow("team", "-t", "-n")]
		public void OptionsAndAliasesShouldParse(string noun, string type, string name)
		{
			var command = Utility.TestParseCommand<ListCommand>("security", noun, "list", type, "Owner", name, "Sales");
			Assert.AreEqual(TeamType.Owner, command.Type);
			Assert.AreEqual("Sales", command.Name);
		}

		[TestMethod]
		[DataRow("0", TeamType.Owner)]
		[DataRow("1", TeamType.Access)]
		[DataRow("2", TeamType.SecurityGroup)]
		[DataRow("3", TeamType.Microsoft365Group)]
		public void NumericTypesShouldParse(string value, TeamType type)
		{
			Assert.AreEqual(type, Utility.TestParseCommand<ListCommand>("security", "teams", "list", "-t", value).Type);
		}

		[TestMethod]
		public void DefaultsShouldIncludeAllTypesAndNames()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "teams", "list");
			Assert.IsNull(command.Type);
			Assert.IsNull(command.Name);
		}

		[TestMethod]
		public void UndefinedTypeShouldFailValidation()
		{
			var command = new ListCommand { Type = (TeamType)4 };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}