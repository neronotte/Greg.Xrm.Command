using Greg.Xrm.Command.Services.Output;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Greg.Xrm.Command
{
    public class AutoUpdater(ILogger log, IOutput output)
    {
		private const string ToolName = "Greg.Xrm.Command";
		private const string NugetUrl = $"https://api.nuget.org/v3-flatcontainer/{ToolName}/index.json";
		private const int WaitForExit = 20;


		public string CurrentVersion => GetType().Assembly.GetName()?.Version?.ToString() ?? "[unable to get version from assembly]";

        public string? NextVersion { get; protected set; }

		public bool UpdateRequired { get; protected set; } = false;


		public async Task<bool> CheckForUpdates()
        {  
			this.NextVersion = null;
			this.UpdateRequired = false;

			#if RELEASE
			try
            {
				using var client = new HttpClient();
				var response = await client.GetStringAsync(NugetUrl);
				using var doc = JsonDocument.Parse(response);
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


		public void LaunchUpdate()
		{
			if (!this.UpdateRequired) return;

			try
			{
				output.WriteLine($"{ToolName} update requested");
				var pid = Process.GetCurrentProcess().Id;
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
