using Greg.Xrm.Command.Commands.Settings.Model;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Settings
{
	[Command("settings", "create", HelpText = "Creates a new setting in the current environment")]
	[Alias("setting", "create")]
	[Alias("setting", "new")]
	[Alias("settings", "new")]
	[Alias("create-setting")]
	[Alias("new-setting")]
	public class CreateCommand
	{
		[Option("displayName", "dn", HelpText = "The name displayed to consumers of the setting in all user interfaces where settings are displayed.")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("name", "n", HelpText = "The unique name of the setting in an environment.\r\nIf not provided, the name is automatically generated based on the display name provided but can be changed before the setting is created. Once a setting is created, the Name can't be changed as it may be referenced in applications or code.\r\nName has a prefix that corresponds to the solution publisher. This prefix is intended to give the setting a unique name if you want to import them into another solution or environment in the future (which would have a different prefix).")]
		public string? Name { get; set; }

		[Option("description", "d", HelpText = "A description of the setting that helps others understand what the setting is used for in all user interfaces where settings are displayed.")]
		public string? Description { get; set; }

		[Option("type", "t", HelpText = "The data type of a setting controls how the setting’s value is stored. Data type can be set to Number, String, or Yes/No. Data type can't be changed after the setting is created.", DefaultValue = SettingDefinitionDataType.String)]
		public SettingDefinitionDataType DataType { get; set; } = SettingDefinitionDataType.String;

		[Option("defaultValue", "dv", HelpText = "The default value of the setting. It specifies the setting's value that will be used unless it is overridden by a setting environment value or a setting app value. It should match the setting type. For booleans you can also provide an int value: 0 means false, any other value means true.")]
		public string? DefaultValue { get; set; }

		[Option("change", "c", HelpText = @"Indicates where the setting can be overridden, at one of the following levels:
- Environment and app, allows both the setting environment value and setting app values to override the default value.
- Environment: allows only the setting environment value to override the default value.
- App only, allows only setting app values to override the default value.",
DefaultValue = OverridableLevel.EnvApp)]
		public OverridableLevel OverridableLevel { get; set; } = OverridableLevel.EnvApp;

		[Option("rel", "r", HelpText = "Release level is used to inform the framework and other consumers of the setting about the state of the feature that the setting is used with. Release level can be set to Generally available or Preview.", DefaultValue = SettingDefinitionReleaseLevel.GA)]
		public SettingDefinitionReleaseLevel ReleaseLevel { get; set; } = SettingDefinitionReleaseLevel.GA;

		[Option("url", "u", HelpText = "A link to documentation to help consumers of the setting understand the purpose of the setting. Will be used as a Learn more link in all user interfaces where settings are displayed.")]
		public string? InformationUrl { get; set; }

		[Option("solution", "s", HelpText = "The solution where to save the created setting. If not specified, the default solution is considered.")]
		public string? SolutionName { get; set; }
	}


	public enum OverridableLevel
	{
		None = 0,
		Env = 1,
		App = 2,
		EnvApp = 3
	}
}
