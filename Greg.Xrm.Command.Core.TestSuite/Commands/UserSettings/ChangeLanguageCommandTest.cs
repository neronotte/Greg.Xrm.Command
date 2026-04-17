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
			Assert.AreEqual(LanguageField.UiLanguageId, command.Field);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage",
				"-l", "1033",
				"-f", "LocaleId");

			Assert.AreEqual(1033, command.Lcid);
			Assert.AreEqual(LanguageField.LocaleId, command.Field);
		}

		[TestMethod]
		public void ParseWithPositionalLcidShouldWork()
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage", "1040");

			Assert.AreEqual(1040, command.Lcid);
			Assert.IsNull(command.Field);
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
		[DataRow("UiLanguageId", LanguageField.UiLanguageId)]
		[DataRow("HelpLanguageId", LanguageField.HelpLanguageId)]
		[DataRow("LocaleId", LanguageField.LocaleId)]
		public void AllFieldValuesShouldBeParseable(string fieldValue, LanguageField expected)
		{
			var command = Utility.TestParseCommand<ChangeLanguageCommand>(
				"usersettings", "changelanguage",
				"--lcid", "1033",
				"--field", fieldValue);

			Assert.AreEqual(expected, command.Field);
		}
	}
}
