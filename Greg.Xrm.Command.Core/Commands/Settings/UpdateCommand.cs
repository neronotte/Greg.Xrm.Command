using Greg.Xrm.Command.Commands.Settings.Model;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Settings
{
	[Command("settings", "update", HelpText = "Updates metadata of a setting")]
	[Alias("settings", "update")]
	[Alias("update-setting")]
	public class UpdateCommand : IValidatableObject
	{
		[Option("name", "n", HelpText = "The unique name of the setting to update.")]
		[Required]
		public string Name { get; set; } = string.Empty;

		[Option("description", "d", HelpText = "If specified, updates the description of the setting.")]
		public string? Description { get; set; }

		[Option("defaultValue", "dv", HelpText = "If specified, updates the default value of the setting. It should match the setting type. For booleans you can also provide an int value: 0 means false, any other value means true.")]
		public string? DefaultValue { get; set; }

		[Option("change", "c", HelpText = @"If specified, updates the overriddable level of the setting.")]
		public OverridableLevel? OverridableLevel { get; set; }

		[Option("rel", "r", HelpText = "If specified, updates the release level of the setting.")]
		public SettingDefinitionReleaseLevel? ReleaseLevel { get; set; }

		[Option("url", "u", HelpText = "If specified, updates the information URL of the setting.")]
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
