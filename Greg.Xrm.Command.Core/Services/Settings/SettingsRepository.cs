using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Settings
{
	public class SettingsRepository : ISettingsRepository
	{
		private string? settingsFolder = null;
		private readonly IStorage storage;

		public SettingsRepository(IStorage storage)
        {
			this.storage = storage;
		}


		private void InitializeSettings()
		{
			if (this.settingsFolder != null) return;
			this.settingsFolder = this.storage.GetOrCreateStorageFolder().FullName;
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

				if (typeof(T) == typeof(string))
					return (T)(object)json;


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
				if (typeof(T) == typeof(string))
				{
					writer.Write(value);
					return Task.CompletedTask;
				}


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
