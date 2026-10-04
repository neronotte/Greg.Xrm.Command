using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.BusinessUnits
{
	[TestClass]
	public class ListCommandTest
	{
		[TestMethod]
		[DataRow("businessunit", "list")]
		[DataRow("bu", "list")]
		[DataRow("bu", "tree")]
		public void CommandAndAliasesShouldDefaultToTree(string noun, string verb)
		{
			var command = Utility.TestParseCommand<ListCommand>("security", noun, verb);
			Assert.AreEqual(BusinessUnitOutputFormat.Tree, command.Format);
		}

		[TestMethod]
		[DataRow("--format", "Tree", BusinessUnitOutputFormat.Tree)]
		[DataRow("--format", "Json", BusinessUnitOutputFormat.Json)]
		[DataRow("-f", "Tree", BusinessUnitOutputFormat.Tree)]
		[DataRow("-f", "Json", BusinessUnitOutputFormat.Json)]
		public void FormatShouldParse(string option, string value, BusinessUnitOutputFormat expected)
		{
			Assert.AreEqual(expected, Utility.TestParseCommand<ListCommand>("security", "businessunit", "list", option, value).Format);
		}

		[TestMethod]
		public void UndefinedFormatShouldFailValidation()
		{
			var command = new ListCommand { Format = (BusinessUnitOutputFormat)2 };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}