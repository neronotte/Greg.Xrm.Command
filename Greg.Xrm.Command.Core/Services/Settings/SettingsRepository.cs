using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Settings
{
	public class SettingsRepository : ISettingsRepository
	{
		private string? settingsFolder = null;

        public SettingsRepository()
        {
		}


		private void InitializeSettings()
		{
			if (this.settingsFolder != null) return;

			var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			folderPath = Path.Combine(folderPath, "Greg.Xrm.Command");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			this.settingsFolder = folderPath;
		}



        public async Task<T?> GetAsync<T>(string key)
		{
			this.InitializeSettings();
			if (this.settingsFolder == null)
				throw new InvalidOperationException("Settings folder is not initialized.");

			var fileName = Path.Combine(this.settingsFolder, $"{key}.json");

			if (File.Exists(fileName))
			{
				var json = await File.ReadAllTextAsync(fileName);
				return JsonConvert.DeserializeObject<T>(json);
			}

			return default;
		}

		public Task SetAsync<T>(string key, T value)
		{
			this.InitializeSettings();
			if (this.settingsFolder == null) 
				throw new InvalidOperationException("Settings folder is not initialized.");

			var fileName = Path.Combine(this.settingsFolder, $"{key}.json");
			using(var writer = new StreamWriter(fileName, false, System.Text.Encoding.UTF8))
			{
				var serializer = new JsonSerializer
				{
					Formatting = Formatting.Indented
				};
				serializer.Serialize(writer, value);
			}

			return Task.CompletedTask;
		}
	}
}
