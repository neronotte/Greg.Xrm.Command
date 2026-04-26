using System.Diagnostics;
using System.ServiceModel;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Services.Connection
{
	public class OrganizationServiceRepository(
		IOutput output,
		ISettingsRepository settings,
		IPacxProjectRepository pacxProjectRepository
		) : IOrganizationServiceRepository
	{
		private readonly ISettingsRepository settings = settings ?? throw new ArgumentNullException(nameof(settings));
		private readonly IPacxProjectRepository pacxProjectRepository = pacxProjectRepository ?? throw new ArgumentNullException(nameof(pacxProjectRepository));

		public string GetTokenCachePath()
		{
			return TokenCache.GetTokenCachePath();
		}


		private static byte[] GetAesKey() => Convert.FromBase64String(Properties.Resources.AesKey);

		private static byte[] GetAesIV() => Convert.FromBase64String(Properties.Resources.AesIV);



		private ConnectionSetting? cache = null;

		private string? _environmentOverride = null;

		public void SetEnvironmentOverride(string nameOrUrl)
		{
			_environmentOverride = nameOrUrl;
		}

		public async Task<string?> GetCurrentEnvironmentOverrideNameAsync()
		{
			if (_environmentOverride == null) return null;
			var (name, _) = await TryResolveOverrideConnectionAsync();
			return name;
		}






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




		/// <summary>
		/// Resolves the connection name and connection string for the current environment override.
		/// Tries profile name first, then URL match.
		/// </summary>
		private async Task<(string connectionName, string connectionString)> TryResolveOverrideConnectionAsync()
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, $"No authentication profile found for environment '{_environmentOverride}'.");

			// 1. Try case-insensitive name match
			var matchedName = connectionStrings.ConnectionStringKeys
				.FirstOrDefault(k => string.Equals(k, _environmentOverride, StringComparison.OrdinalIgnoreCase));

			if (matchedName != null
				&& connectionStrings.TryGetConnectionString(matchedName, GetAesKey(), GetAesIV(), out var connectionString)
				&& !string.IsNullOrWhiteSpace(connectionString))
			{
				return (matchedName, connectionString!);
			}

			// 2. Try URL match
			var normalizedOverride = _environmentOverride!.TrimEnd('/').ToUpperInvariant();
			foreach (var name in connectionStrings.ConnectionStringKeys)
			{
				if (!connectionStrings.TryGetConnectionString(name, GetAesKey(), GetAesIV(), out var cs) || string.IsNullOrWhiteSpace(cs))
					continue;

				var parts = cs!.Split(';').ToList();
				var urlToken = parts.Find(p => p.StartsWith("Url", StringComparison.OrdinalIgnoreCase))
								?? parts.Find(p => p.StartsWith("ServiceUri", StringComparison.OrdinalIgnoreCase))
								?? parts.Find(p => p.StartsWith("Service Uri", StringComparison.OrdinalIgnoreCase))
								?? parts.Find(p => p.StartsWith("Server", StringComparison.OrdinalIgnoreCase));

				if (string.IsNullOrWhiteSpace(urlToken)) continue;

				var eqIdx = urlToken.IndexOf('=');
				if (eqIdx < 0) continue;

				var storedUrl = urlToken[(eqIdx + 1)..].Trim().TrimEnd('/');
				if (storedUrl.Equals(normalizedOverride, StringComparison.OrdinalIgnoreCase))
					return (name, cs!);
			}

			throw new CommandException(CommandException.ConnectionNotSet, $"No authentication profile found for environment '{_environmentOverride}'.");
		}


		public async Task<string> GetCurrentConnectionNameAsync()
		{
			if (_environmentOverride != null)
			{
				var (name, _) = await TryResolveOverrideConnectionAsync();
				return name;
			}

			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			bool found;
			string? connectionName;
			var project = await this.pacxProjectRepository.GetCurrentProjectAsync();
			if (project != null && !project.IsSuspended)
			{
				connectionName = project.AuthProfileName;
				found = connectionStrings.TryGetConnectionString(project.AuthProfileName, GetAesKey(), GetAesIV(), out _);
				if (!found)
					throw new CommandException(CommandException.ConnectionNotSet, $"Unable to find an authentication profile called {project.AuthProfileName}.");
			}
			else
			{
				connectionName = connectionStrings.CurrentConnectionStringKey;
				found = connectionStrings.TryGetCurrentConnectionString(GetAesKey(), GetAesIV(), out _);
				if (!found)
					throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");
			}

			return connectionName!;
		}

		public async Task<IOrganizationServiceAsync2> GetCurrentConnectionAsync()
		{
			if (_environmentOverride != null)
			{
				var (overrideName, overrideConnectionString) = await TryResolveOverrideConnectionAsync();
				return await this.CreateServiceClientAsync(overrideName, overrideConnectionString);
			}

			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			string? connectionString;
			bool found;
			string? connectionName;
			var project = await this.pacxProjectRepository.GetCurrentProjectAsync();
			if (project != null && !project.IsSuspended)
			{
				connectionName = project.AuthProfileName;
				found = connectionStrings.TryGetConnectionString(project.AuthProfileName, GetAesKey(), GetAesIV(), out connectionString);
				if (!found)
					throw new CommandException(CommandException.ConnectionNotSet, $"Unable to find an authentication profile called {project.AuthProfileName}.");
			}
			else
			{
				connectionName = connectionStrings.CurrentConnectionStringKey;
				found = connectionStrings.TryGetCurrentConnectionString(GetAesKey(), GetAesIV(), out connectionString);
				if (!found)
					throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");
			}

			return await this.CreateServiceClientAsync(connectionName!, connectionString!);
		}





		public async Task<IOrganizationServiceAsync2> GetConnectionByName(string connectionName)
		{
			var connectionStrings = await GetConnectionSettingAsync()
				?? throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			if (!connectionStrings.TryGetConnectionString(connectionName, GetAesKey(), GetAesIV(), out var connectionString))
				throw new CommandException(CommandException.ConnectionNotSet, "Dataverse connection has not been set yet.");

			return await this.CreateServiceClientAsync(connectionName, connectionString!);
		}




		public async Task SetConnectionAsync(string name, string connectionString)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name), "The connection name is empty.");
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException(nameof(connectionString), "The connection string is empty.");

			try
			{
				var crm = await CreateServiceClientAsync(name, connectionString);
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
			await TokenCache.ClearAccessTokenAsync(name);

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
			if (_environmentOverride != null)
			{
				var (overrideName, _) = await TryResolveOverrideConnectionAsync();
				var overrideConnections = await GetConnectionSettingAsync()
					?? throw new CommandException(CommandException.ConnectionInvalid, "No connection has been set yet.");
				overrideConnections.DefaultSolutions.TryGetValue(overrideName, out var overrideSolution);
				return overrideSolution;
			}

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



		/// <summary>
		/// This method tries to create a service client
		/// </summary>
		/// <param name="connectionName"></param>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		private async Task<ServiceClient> CreateServiceClientAsync(string connectionName, string connectionString)
		{
			var accessToken = await TokenCache.TryGetAccessTokenAsync(connectionName);
			if (accessToken == null)
			{
				return await CreateServiceClientFromConnectionString(connectionName, connectionString);
			}


			var crm = new ServiceClient(accessToken.ServiceUri, uri => Task.FromResult(accessToken.AccessToken));
			if (!crm.IsReady)
			{
				return await CreateServiceClientFromConnectionString(connectionName, connectionString);
			}

			try
			{
				await crm.ExecuteAsync(new WhoAmIRequest());
			}
			catch (DataverseConnectionException)
			{
				return await CreateServiceClientFromConnectionString(connectionName, connectionString);
			}
			catch (System.ServiceModel.Security.MessageSecurityException)
			{
				return await CreateServiceClientFromConnectionString(connectionName, connectionString);
			}
			catch (Exception ex)
			{
				Debug.Print(ex.GetType().FullName);
			}

			return crm;
		}


		private void RecourseInnerExceptions(Exception ex, int level)
		{
			if (ex?.InnerException != null && level <= 10)
			{
				output.WriteLine($"Inner exception ({level}): {ex.InnerException.Message}", ConsoleColor.Yellow);
				RecourseInnerExceptions(ex.InnerException, level + 1);
			}
		}

		private async Task<ServiceClient> CreateServiceClientFromConnectionString(string connectionName, string connectionString)
		{

			try
			{
				var crm = new ServiceClient(this.GetFullConnectionString(connectionString));
				if (crm.ActiveAuthenticationType == Microsoft.PowerPlatform.Dataverse.Client.AuthenticationType.OAuth
					&& !string.IsNullOrWhiteSpace(crm.CurrentAccessToken))
				{
					await TokenCache.SaveAccessTokenAsync(connectionName, crm.ConnectedOrgUriActual, crm.CurrentAccessToken);
				}
				else
				{
					await TokenCache.ClearAccessTokenAsync(connectionName);
				}
				return crm;
			}
			catch (DataverseConnectionException ex)
			{
				output.WriteLine($"Failed to create ServiceClient for connection '{connectionName}': {ex.Message}", ConsoleColor.Yellow);
				RecourseInnerExceptions(ex, 1);
				throw;
			}
		}
	}
}
