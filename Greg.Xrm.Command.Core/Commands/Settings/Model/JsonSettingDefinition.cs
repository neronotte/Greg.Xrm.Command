using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Greg.Xrm.Command.Commands.Settings.Model
{
	public class JsonSettingDefinition
	{
		public JsonSettingDefinition(SettingDefinition settingDefinition, OrganizationSetting? organizationSetting, IReadOnlyCollection<AppSetting>? appSettings)
		{
			this.uniquename = settingDefinition.uniquename;
			this.displayname = settingDefinition.displayname;
			this.description = settingDefinition.description;
			this.datatype = settingDefinition.FormattedDataType ?? string.Empty;
			this.defaultvalue = settingDefinition.defaultvalue;
			this.informationurl = settingDefinition.informationurl;
			this.isoverridable = settingDefinition.isoverridable;
			this.isvisible = !settingDefinition.ishidden;
			this.overridablelevel = settingDefinition.FormattedOverridableLevel ?? string.Empty;
			this.releaselevel = settingDefinition.FormattedReleaseLevel ?? string.Empty;
			this.environmentvalue = organizationSetting?.value;

			if (appSettings != null && appSettings.Count > 0)
			{
				var dict = appSettings.ToDictionary(x => x.FormattedAppModule ?? string.Empty, x => x.value);
				this.appvalues = dict.ToImmutableSortedDictionary();
			}
		}

		[JsonProperty(Order = 1)]
		public string uniquename { get; set; }
		[JsonProperty(Order = 2)]
		public string displayname { get; set; }
		[JsonProperty(Order = 3)]
		public string? description { get; set; }
		[JsonProperty(Order = 4)]
		public string datatype { get; set; }
		[JsonProperty(Order = 5)]
		public bool isoverridable { get; set; }
		[JsonProperty(Order = 6)]
		public string overridablelevel { get; set; }
		[JsonProperty(Order = 7)]
		public bool isvisible { get; set; }
		[JsonProperty(Order = 8)]
		public string releaselevel { get; set; }
		[JsonProperty(Order = 9)]
		public string? informationurl { get; set; }


		[JsonProperty(Order = 10)]
		public string? defaultvalue { get; set; }
		[JsonProperty(Order = 11)]
		public string? environmentvalue { get; set; }

		[JsonProperty(Order = 12)]
		public ImmutableSortedDictionary<string, string>? appvalues { get; set; }
	}
}
