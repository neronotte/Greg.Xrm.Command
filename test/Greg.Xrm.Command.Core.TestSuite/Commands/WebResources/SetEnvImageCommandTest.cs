using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[TestClass]
	public class SetEnvImageCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredOnlyShouldWork()
		{
			var command = Utility.TestParseCommand<SetEnvImageCommand>(
				"webresources", "setEnvImage",
				"--name", "new_logo.png");

			Assert.AreEqual("new_logo.png", command.WebResourceUniqueName);
			Assert.IsNull(command.AppId);
			Assert.IsNull(command.AppName);
			Assert.IsNull(command.LocalThemeFile);
		}

		[TestMethod]
		public void ParseWithAppIdAndLocalThemeFileShouldWork()
		{
			var command = Utility.TestParseCommand<SetEnvImageCommand>(
				"webresources", "setEnvImage",
				"--name", "new_logo.png",
				"--appId", "00000000-0000-0000-0000-000000000001",
				"--localThemeFile", "new_/themes/theme.xml");

			Assert.AreEqual("00000000-0000-0000-0000-000000000001", command.AppId);
			Assert.AreEqual("new_/themes/theme.xml", command.LocalThemeFile);
		}

		[TestMethod]
		public void ParseWithAppNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetEnvImageCommand>(
				"wr", "setLogo",
				"-n", "new_logo.png",
				"--appName", "Sales Hub");

			Assert.AreEqual("Sales Hub", command.AppName);
		}

		[TestMethod]
		public void ValidateShouldFailWhenBothAppIdAndAppNameAreProvided()
		{
			var command = new SetEnvImageCommand
			{
				WebResourceUniqueName = "new_logo.png",
				AppId = "00000000-0000-0000-0000-000000000001",
				AppName = "Sales Hub"
			};

			var results = command.Validate(new ValidationContext(command)).ToList();

			Assert.AreEqual(1, results.Count);
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(SetEnvImageCommand.AppId));
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(SetEnvImageCommand.AppName));
		}

		[TestMethod]
		public void ValidateShouldFailWhenAppIdIsNotGuid()
		{
			var command = new SetEnvImageCommand
			{
				WebResourceUniqueName = "new_logo.png",
				AppId = "not-a-guid"
			};

			var results = command.Validate(new ValidationContext(command)).ToList();
			Assert.AreEqual(1, results.Count);
			CollectionAssert.Contains(results[0].MemberNames.ToList(), nameof(SetEnvImageCommand.AppId));
		}
	}
}
