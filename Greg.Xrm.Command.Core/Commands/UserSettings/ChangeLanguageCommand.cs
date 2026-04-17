using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public enum UserSettingsLanguageField
	{
		UiLanguageId,
		HelpLanguageId,
		LocaleId
	}

	[Command("usersettings", "changelanguage", HelpText = "Changes language settings for the current user.")]
	public class ChangeLanguageCommand
	{
		[Option("lcid", "l", Order = 1, HelpText = "The language LCID to set.")]
		[Required]
		public int Lcid { get; set; }

		[Option("field", "f", Order = 2, HelpText = "Field to update: UiLanguageId, HelpLanguageId, or LocaleId. If omitted, all fields are updated.")]
		public UserSettingsLanguageField? Field { get; set; }
	}
}
