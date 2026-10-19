using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[TestClass]
	public class ProfileCommandTest
	{
		[TestMethod]
		[DataRow("user")]
		[DataRow("users")]
		public void AliasesShouldParse(string noun)
		{
			Assert.AreEqual("john@contoso.com", Utility.TestParseCommand<ProfileCommand>(noun, "profile", "--user", "john@contoso.com").User);
			Assert.AreEqual("john@contoso.com", Utility.TestParseCommand<ProfileCommand>("security", noun, "profile", "-u", "john@contoso.com").User);
		}

		[TestMethod]
		[DataRow("--format", "Tree", ProfileOutputFormat.Tree)]
		[DataRow("-f", "Json", ProfileOutputFormat.Json)]
		public void FormatShouldParse(string option, string value, ProfileOutputFormat expected)
		{
			Assert.AreEqual(expected, Utility.TestParseCommand<ProfileCommand>("user", "profile", option, value).Format);
		}

		[TestMethod]
		public void DefaultsShouldUseCurrentUserAndTree()
		{
			var command = Utility.TestParseCommand<ProfileCommand>("user", "profile");
			Assert.IsNull(command.User);
			Assert.AreEqual(ProfileOutputFormat.Tree, command.Format);
		}

		[TestMethod]
		public void UndefinedFormatShouldFailValidation()
		{
			var command = new ProfileCommand { Format = (ProfileOutputFormat)2 };
			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), [], true));
		}
	}
}