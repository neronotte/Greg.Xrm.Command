namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class SetCommandTest
	{
		// ?? Single setting ????????????????????????????????????????????????????????

		[TestMethod]
		public void ParseWithSingleIntegerSettingShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--uilanguageid", "1040");

			Assert.AreEqual(1040, command.UILanguageId);
			Assert.IsNull(command.UserDomainName);
		}

		[TestMethod]
		public void ParseWithBooleanSettingShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--showweeknumber", "true");

			Assert.AreEqual(true, command.ShowWeekNumber);
		}

		[TestMethod]
		public void ParseEnumByNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--entityformmode", "ReadOptimized");

			Assert.AreEqual(SetCommand.FormMode.ReadOptimized, command.EntityFormModeValue);
		}

		[TestMethod]
		public void ParseEnumByNumericCodeShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--entityformmode", "1");

			Assert.AreEqual(SetCommand.FormMode.ReadOptimized, command.EntityFormModeValue);
		}

		// ?? Multiple settings in a single call ???????????????????????????????????

		[TestMethod]
		public void ParseWithMultipleSettingsShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--uilanguageid", "1033",
				"--helplanguageid", "1033",
				"--paginglimit", "250");

			Assert.AreEqual(1033, command.UILanguageId);
			Assert.AreEqual(1033, command.HelpLanguageId);
			Assert.AreEqual(250, command.PagingLimit);

			var provided = command.GetProvidedSettings();
			Assert.AreEqual(3, provided.Count);
			Assert.AreEqual(1033, provided["uilanguageid"]);
			Assert.AreEqual(1033, provided["helplanguageid"]);
			Assert.AreEqual(250, provided["paginglimit"]);
		}

		[TestMethod]
		public void GetProvidedSettingsNormalisesEnumToItsNumericCode()
		{
			var command = new SetCommand { EntityFormModeValue = SetCommand.FormMode.Edit };
			var provided = command.GetProvidedSettings();

			Assert.AreEqual(1, provided.Count);
			Assert.AreEqual(2, provided["entityformmode"]);
		}

		[TestMethod]
		public void GetProvidedSettingsKeepsBooleanTyped()
		{
			var command = new SetCommand { ShowWeekNumber = true, IgnoreUnsolicitedEmail = false };
			var provided = command.GetProvidedSettings();

			Assert.AreEqual(true, provided["showweeknumber"]);
			Assert.AreEqual(false, provided["ignoreunsolicitedemail"]);
		}

		// ?? --user / -u ???????????????????????????????????????????????????????????

		[TestMethod]
		public void UserOptionWithLongNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--user", @"DOMAIN\john.doe",
				"--showweeknumber", "true");

			Assert.AreEqual(@"DOMAIN\john.doe", command.UserDomainName);
			Assert.AreEqual(true, command.ShowWeekNumber);
		}

		[TestMethod]
		public void UserOptionWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"-u", "john.doe@contoso.com",
				"--timezonecode", "85");

			Assert.AreEqual("john.doe@contoso.com", command.UserDomainName);
			Assert.AreEqual(85, command.TimeZoneCode);
		}

		[TestMethod]
		public void UserShouldBeNullWhenOmitted()
		{
			var command = Utility.TestParseCommand<SetCommand>(
				"usersettings", "set",
				"--paginglimit", "100");

			Assert.IsNull(command.UserDomainName);
			Assert.AreEqual(100, command.PagingLimit);
		}

		// ?? Validation ????????????????????????????????????????????????????????????

		[TestMethod]
		public void GetProvidedSettingsShouldBeEmptyWhenNothingSet()
		{
			var command = new SetCommand();
			Assert.AreEqual(0, command.GetProvidedSettings().Count);
		}

		[TestMethod]
		public void ValidateShouldFailWhenNoSettingsProvided()
		{
			var command = new SetCommand();
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("At least one user setting")));
		}

		[TestMethod]
		public void ValidateShouldFailOnInvalidLcid()
		{
			var command = new SetCommand { UILanguageId = -1 };
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("not a recognised Windows culture")));
		}

		[TestMethod]
		public void ValidateShouldFailOnMalformedWorkdayTime()
		{
			var command = new SetCommand { WorkdayStartTime = "bogus" };
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.ErrorMessage!.Contains("HH:mm")));
		}

		[TestMethod]
		public void ValidateShouldSucceedOnWellFormedWorkdayTime()
		{
			var command = new SetCommand { WorkdayStartTime = "08:00" };
			var results = Validate(command);

			Assert.IsFalse(results.Any(r => r.ErrorMessage!.Contains("HH:mm")));
		}

		[TestMethod]
		public void ValidateShouldFailOnPagingLimitBelowRange()
		{
			var command = new SetCommand { PagingLimit = -5 };
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(SetCommand.PagingLimit))));
		}

		[TestMethod]
		public void ValidateShouldFailOnTooLongString()
		{
			var command = new SetCommand { CurrencySymbol = new string('X', 20) };
			var results = Validate(command);

			Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(SetCommand.CurrencySymbol))));
		}

		private static IReadOnlyList<System.ComponentModel.DataAnnotations.ValidationResult> Validate(SetCommand command)
		{
			var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(command);
			var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
			System.ComponentModel.DataAnnotations.Validator.TryValidateObject(command, ctx, results, validateAllProperties: true);
			return results;
		}
	}
}
