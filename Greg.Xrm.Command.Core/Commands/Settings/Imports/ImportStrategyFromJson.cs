using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public class ImportStrategyFromJson : IImportStrategy
	{
		private readonly Stream stream;

		public ImportStrategyFromJson(Stream stream)
        {
			this.stream = stream;
		}

        public async Task<IReadOnlyList<IImportAction>> ImportAsync(CancellationToken cancellationToken)
		{
			var actions = new List<IImportAction>();

			var text = await new StreamReader(this.stream).ReadToEndAsync(cancellationToken);
			
			var settings = JsonConvert.DeserializeObject<Model[]>(text);

			if (settings == null || settings.Length == 0)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "The provided file does not contain any setting to update.");
			}

			CheckDuplicates(settings);

			foreach (var item in settings)
			{
				if (item.DefaultValue != null)
				{
					actions.Add(new ImportActionSetDefaultValue(item.UniqueName, item.DefaultValue));
				}

				if (item.EnvironmentValue != null)
				{
					actions.Add(new ImportActionSetEnvironmentValue(item.UniqueName, item.EnvironmentValue));
				}

				if (item.AppValues != null)
				{
					foreach (var key in item.AppValues.Where(x => x.Value != null))
					{
						actions.Add(new ImportActionSetAppValue(item.UniqueName, key.Key, key.Value));
					}
				}

			}

			return actions;
		}

		private static void CheckDuplicates(Model[] settings)
		{
			var duplicatedSettings = settings
				.GroupBy(x => x.UniqueName.ToLowerInvariant())
				.Where(x => x.Count() > 1)
				.Select(x => x.Key)
				.OrderBy(x => x)
				.ToList();

			if (duplicatedSettings.Count > 0)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The provided file contains duplicated settings: {string.Join(", ", duplicatedSettings)}");
			}
		}

		class Model
		{
			[JsonProperty("uniquename", Required = Required.Always)]
			public string UniqueName { get; set; } = string.Empty;

			[JsonProperty("defaultvalue", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
			public string? DefaultValue { get; set; }

			[JsonProperty("environmentvalue", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
			public string? EnvironmentValue { get; set; }

			[JsonProperty("appvalues", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
			public Dictionary<string, string>? AppValues { get; set; }
		}
	}
}