using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Greg.Xrm.Command.Updates
{
	public class AutoUpdater(ILogger<AutoUpdater> log, IOutput output, ISettingsRepository settings) : IAutoUpdater
    {
		private const string ToolName = "Greg.Xrm.Command";
		public const string EnableAutoUpdateSettingKey = "IsAutoUpdateEnabled";
		private const int WaitForExit = 20;


		public string CurrentVersion => GetType().Assembly.GetName()?.Version?.ToString() ?? "[unable to get version from assembly]";

        public string? NextVersion { get; protected set; }

		public bool UpdateRequired { get; protected set; } = false;


		public async Task<bool> CheckForUpdatesAsync()
        {  
			this.NextVersion = null;
			this.UpdateRequired = false;

#if RELEASE
			var isEnabled = await settings.GetAsync<bool>(EnableAutoUpdateSettingKey);
			if (!isEnabled) return false;
			
			try
            {
				var nugetUrl = $"https://api.nuget.org/v3-flatcontainer/{ToolName.ToLowerInvariant()}/index.json";

				using var client = new HttpClient();
				var response = await client.GetStringAsync(nugetUrl);
				using var doc = System.Text.Json.JsonDocument.Parse(response);
				var versions = doc.RootElement.GetProperty("versions").EnumerateArray();
				var latestVersion = versions.Last().GetString();

				if (latestVersion != CurrentVersion)
				{
					log.LogDebug("A new version ({NextVersion}) is available. It will be installed after this run.", latestVersion);
					this.UpdateRequired = true;
					this.NextVersion = latestVersion;
				}
				else
				{
					log.LogDebug("You are using the latest version.");
					this.UpdateRequired = false;
				}
			}
            catch(Exception ex)
            {
				log.LogError(ex, "Error while checking for updates: {Message}", ex.Message);
				this.UpdateRequired = false;
			}

#endif

			return this.UpdateRequired;
		}


		public async Task LaunchUpdateAsync()
		{
			if (!this.UpdateRequired) return;

			var isEnabled = await settings.GetAsync<bool>(EnableAutoUpdateSettingKey);
			if (!isEnabled) return;

			try
			{
				output.WriteLine($"{ToolName} update requested");
				var pid = Environment.ProcessId;
				var command = $@"Wait-Process -Id {pid} -Timeout {WaitForExit} -ErrorAction SilentlyContinue; dotnet tool update --global {ToolName}";
				Process.Start(new ProcessStartInfo
				{
					FileName = "powershell.exe",
					Arguments = $"-NoProfile -NoLogo -NonInteractive -ExecutionPolicy unrestricted -command {command}",
					UseShellExecute = false
				});
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error while launching update: {Message}", ex.Message);
			}
		}
	}
}
