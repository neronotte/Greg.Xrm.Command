namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class ChangeLanguageCommandTest
	{
		[TestMethod]
		public void ParseWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage",
				"--lcid", "1040",
				"--field", "UiLanguageId");

			Assert.AreEqual(1040, command.Lcid);
			Assert.AreEqual(UserSettingsLanguageField.UiLanguageId, command.Field);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage",
				"-l", "1033",
				"-f", "LocaleId");

			Assert.AreEqual(1033, command.Lcid);
			Assert.AreEqual(UserSettingsLanguageField.LocaleId, command.Field);
		}

		[TestMethod]
		public void FieldShouldDefaultToNullWhenOmitted()
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage",
				"--lcid", "1033");

			Assert.AreEqual(1033, command.Lcid);
			Assert.IsNull(command.Field);
		}

		[TestMethod]
		[DataRow("UiLanguageId", UserSettingsLanguageField.UiLanguageId)]
		[DataRow("HelpLanguageId", UserSettingsLanguageField.HelpLanguageId)]
		[DataRow("LocaleId", UserSettingsLanguageField.LocaleId)]
		public void AllFieldValuesShouldBeParseable(string fieldValue, UserSettingsLanguageField expected)
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage",
				"--lcid", "1033",
				"--field", fieldValue);

			Assert.AreEqual(expected, command.Field);
		}
	}
}
