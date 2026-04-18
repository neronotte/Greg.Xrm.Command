using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Commands.Settings.Model;

namespace Greg.Xrm.Command.Commands.Settings
{
	[Command("settings", "update", HelpText = "Updates metadata of a setting")]
	[Alias("settings", "update")]
	[Alias("update-setting")]
	public class UpdateCommand : IValidatableObject
	{
		[Option("name", "n", Order = 1, HelpText = "The unique name of the setting to update.")]
		[Required]
		public string Name { get; set; } = string.Empty;

		[Option("description", "d", Order = 2, HelpText = "If specified, updates the description of the setting.")]
		public string? Description { get; set; }

		[Option("defaultValue", "dv", Order = 3, HelpText = "If specified, updates the default value of the setting. It should match the setting type. For booleans you can also provide an int value: 0 means false, any other value means true.")]
		public string? DefaultValue { get; set; }

		[Option("change", "c", Order = 4, HelpText = @"If specified, updates the overriddable level of the setting.")]
		public OverridableLevel? OverridableLevel { get; set; }

		[Option("rel", "r", Order = 5, HelpText = "If specified, updates the release level of the setting.")]
		public SettingDefinitionReleaseLevel? ReleaseLevel { get; set; }

		[Option("url", "u", Order = 6, HelpText = "If specified, updates the information URL of the setting.")]
		public string? InformationUrl { get; set; }


		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (
				(Description == null)
				&& (DefaultValue == null)
				&& (OverridableLevel == null)
				&& (ReleaseLevel == null)
				&& (InformationUrl == null))
			{
				yield return new ValidationResult("At least one property to update must be specified.", new[] { nameof(Description), nameof(DefaultValue), nameof(OverridableLevel), nameof(ReleaseLevel), nameof(InformationUrl) });
			}
		}
	}
}
