using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Services.Connection
{
	public class OrganizationServiceRepository(
		ISettingsRepository settings,
		IPacxProjectRepository pacxProjectRepository
		) : IOrganizationServiceRepository
	{
		private readonly ISettingsRepository settings = settings ?? throw new ArgumentNullException(nameof(settings));
		private readonly IPacxProjectRepository pacxProjectRepository = pacxProjectRepository ?? throw new ArgumentNullException(nameof(pacxProjectRepository));

		public string GetTokenCachePath()
		{
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

			folderPath = Path.Combine(folderPath, "tokenCache");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			return folderPath;
		}


		private static byte[] GetAesKey() => Convert.FromBase64String(Properties.Resources.AesKey);

		private static byte[] GetAesIV() => Convert.FromBase64String(Properties.Resources.AesIV);

		private ConnectionSetting? cache = null;






		private async Task<ConnectionSetting?> GetConnectionSettingAsync()
		{
			if (this.cache != null) return this.cache;

			var connectionSettings = await this.settings.GetAsync<ConnectionSetting>("connections");

			if (connectionSettings != null && !connectionSettings.IsSecured.GetValueOrDefault())
			{
				connectionSettings.SecureSettings(GetAesKey(), GetAesIV());
				await this.settings.SetAsync("connections", connectionSettings);
			}
			cache = connectionSettings;
			return connectionSettings;
		}





		/// <summary>
		/// Given the name of an authentication profile, it returns the environment name.
		/// </summary>
		/// <param name="connectionName">The name of the authentication profile.</param>
		/// <returns></returns>
		public async Task<string?> GetEnvironmentFromConnectioStringAsync(string connectionName)
		{
			var connectionStrings = await GetConnectionSettingAsync();
			if (connectionStrings == null) return null;

			if (!connectionStrings.TryGetConnectionString(connectionName, GetAesKey(), GetAesIV(), out var connectionString))
				return null;

			if (string.IsNullOrWhiteSpace(connectionString))
				return null;

			var parts = connectionString.Split(';').ToList();

			var environmentToken = parts.Find(p => p.StartsWith("Url", StringComparison.OrdinalIgnoreCase))
									?? parts.Find(p => p.StartsWith("ServiceUri", StringComparison.OrdinalIgnoreCase))
									?? parts.Find(p => p.StartsWith("Service Uri", StringComparison.OrdinalIgnoreCase))
									?? parts.Find(p => p.StartsWith("Server", StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(environmentToken))
				return null;

			var startIndex = environmentToken.IndexOf('=') + 1;
			if (startIndex < 0)
				return null;

			var value = environmentToken[startIndex..]?.Trim();
			return value;
		}



		public async Task<ConnectionSetting> GetAllConnectionDefinitionsAsync()
		{
			var connectionStrings = await GetConnectionSettingAsync();
			return connectionStrings ?? new ConnectionSetting() { IsSecured = true };
		}





		public async Task<IOrganizationServiceAsync2> GetCurrentConnectionAsync()
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			string? connectionString;
			bool found;
			var project = await this.pacxProjectRepository.GetCurrentProjectAsync();
			if (project != null && !project.IsSuspended)
			{
				found = connectionStrings.TryGetConnectionString(project.AuthProfileName, GetAesKey(), GetAesIV(), out connectionString);
				if (!found)
					throw new CommandException(CommandException.ConnectionNotSet, $"Unable to find an authentication profile called {project.AuthProfileName}.");
			}
			else
			{
				found = connectionStrings.TryGetCurrentConnectionString(GetAesKey(), GetAesIV(), out connectionString);
				if (!found)
					throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");
			}

			return new ServiceClient(this.GetFullConnectionString(connectionString));
		}





		public async Task<IOrganizationServiceAsync2> GetConnectionByName(string connectionName)
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			if (!connectionStrings.TryGetConnectionString(connectionName, GetAesKey(), GetAesIV(), out var connectionString))
				throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			return new ServiceClient(this.GetFullConnectionString(connectionString));
		}




		public async Task SetConnectionAsync(string name, string connectionString)
		{
			try
			{
				var crm = new ServiceClient(this.GetFullConnectionString(connectionString));
				await crm.ExecuteAsync(new WhoAmIRequest());


				var connectionStrings = await GetConnectionSettingAsync();
				connectionStrings ??= new ConnectionSetting() { IsSecured = true };

				connectionStrings.UpsertConnectionString(name, connectionString, GetAesKey(), GetAesIV());
				connectionStrings.CurrentConnectionStringKey = name;

				await this.settings.SetAsync("connections", connectionStrings);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				throw new CommandException(CommandException.ConnectionInvalid, "Dataverse connection has not been set yet.", ex);
			}
		}





		public async Task RenameConnectionAsync(string oldName, string newName)
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, "No connections set, nothing to update");
			if (!connectionStrings.Exists(oldName))
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "Invalid connection name: " + oldName);
			}

			if (connectionStrings.Exists(newName))
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The new connection name '{newName}' is already in use!");
			}

			connectionStrings.Rename(oldName, newName);

			await this.settings.SetAsync("connections", connectionStrings);
		}




		public async Task DeleteConnectionAsync(string name)
		{
			var connectionStrings = await this.settings.GetAsync<ConnectionSetting>("connections")
				?? throw new CommandException(CommandException.ConnectionNotSet, "No connections set, nothing to update");
			if (!connectionStrings.Exists(name))
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "Invalid connection name: " + name);
			}

			connectionStrings.Remove(name);

			await this.settings.SetAsync("connections", connectionStrings);
		}




		public async Task SetDefaultAsync(string name)
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			if (!connectionStrings.Exists(name))
				throw new CommandException(CommandException.ConnectionInvalid, "Invalid connection name: " + name);

			if (connectionStrings.CurrentConnectionStringKey == name)
				return; // already set as default

			connectionStrings.CurrentConnectionStringKey = name;
			await this.settings.SetAsync("connections", connectionStrings);
		}



		public async Task SetDefaultSolutionAsync(string uniqueName)
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");
			if (connectionStrings.CurrentConnectionStringKey == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			connectionStrings.DefaultSolutions[connectionStrings.CurrentConnectionStringKey] = uniqueName;
			await this.settings.SetAsync("connections", connectionStrings);
		}

		public async Task<string?> GetCurrentDefaultSolutionAsync()
		{
			var project = await this.pacxProjectRepository.GetCurrentProjectAsync();
			if (project != null && !project.IsSuspended)
			{
				return project.SolutionName;
			}


			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");


			if (connectionStrings.CurrentConnectionStringKey == null)
				throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");

			if (!connectionStrings.DefaultSolutions.TryGetValue(connectionStrings.CurrentConnectionStringKey, out var uniqueName))
				return null;

			return uniqueName;
		}


		private string GetFullConnectionString(string? connectionString)
		{
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException(nameof(connectionString), "The connection string is empty.");

			if (connectionString.Contains("TokenCacheStorePath="))
				return connectionString;

			if (!connectionString.EndsWith(';'))
				connectionString += ";";

			return connectionString + "TokenCacheStorePath=" + GetTokenCachePath();
		}
	}
}
