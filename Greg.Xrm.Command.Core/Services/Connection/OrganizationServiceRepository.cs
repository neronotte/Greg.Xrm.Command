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
	/// <summary>
	/// Stores the authentication profiles and creates the Dataverse connections used by every command.
	/// </summary>
	/// <remarks>
	/// <para>
	/// TOKEN CACHING (issue #217, see docs/issue-217-auth-token-implementation-plan.md).
	/// Before #217 an OAuth profile reused the last access token stored in plain text in <c>tokens.json</c>
	/// through a <see cref="ServiceClient"/> built from a fixed-token callback. That client can never refresh the
	/// token, so commands running longer than the token lifetime (about one hour) failed with
	/// "unauthorized ... scheme 'Anonymous'". The token was also exposed in plain text on disk.
	/// </para>
	/// <para>
	/// Each connection now follows one of the paths described by <see cref="TokenCacheMode"/>:
	/// OAuth profiles on systems with OS secure storage let the SDK own the encrypted MSAL cache (persisted,
	/// shared across processes, refreshed automatically); OAuth profiles without secure storage and all
	/// non-interactive profiles keep the exact pre-#217 code path, so nothing regresses for them.
	/// </para>
	/// </remarks>
	public class OrganizationServiceRepository(
		IOutput output,
		ISettingsRepository settings,
		IPacxProjectRepository pacxProjectRepository,
		ISecureTokenStorageProbe? secureTokenStorageProbe = null
		) : IOrganizationServiceRepository
	{
		private readonly ISettingsRepository settings = settings ?? throw new ArgumentNullException(nameof(settings));
		private readonly IPacxProjectRepository pacxProjectRepository = pacxProjectRepository ?? throw new ArgumentNullException(nameof(pacxProjectRepository));

		// Issue #217: tells whether OAuth profiles can use the encrypted SDK MSAL cache on this machine.
		// Optional so that the DI registration and existing callers need no change; tests inject a fake.
		private readonly ISecureTokenStorageProbe secureTokenStorageProbe = secureTokenStorageProbe ?? new MsalSecureTokenStorageProbe(TokenCache.GetTokenCachePath);

		/// <summary>
		/// Returns the PACX token cache DIRECTORY.
		/// </summary>
		/// <remarks>
		/// Kept as-is for compatibility with plugins, even though the interface describes it as the MSAL cache file:
		/// changing the returned value could break callers that combine it with their own file names.
		/// The MSAL cache file actually used is <see cref="TokenCache.GetMsalCacheFilePath"/>.
		/// </remarks>
		public string GetTokenCachePath()
		{
			return TokenCache.GetTokenCachePath();
		}


		internal static byte[] GetAesKey() => Convert.FromBase64String(Properties.Resources.AesKey);

		internal static byte[] GetAesIV() => Convert.FromBase64String(Properties.Resources.AesIV);



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


		public async Task<string?> GetCurrentAccessTokenAsync()
		{
			var conn = (ServiceClient)await GetCurrentConnectionAsync();
			if (!string.IsNullOrWhiteSpace(conn.CurrentAccessToken)) return conn.CurrentAccessToken;

			var connectionName = await GetCurrentConnectionNameAsync();
			var accessToken = await TokenCache.TryGetAccessTokenAsync(connectionName);
			return accessToken?.AccessToken;
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

				// Issue #217: store the UPN of the account that just signed in. The SDK needs it to find that account
				// in its MSAL cache in later processes; without it every command would require a new browser sign-in.
				// Only added to OAuth profiles that do not already specify a user (see OAuthConnectionString).
				if (OAuthConnectionString.TryAddUsername(connectionString, crm.OAuthUserId, out var connectionStringWithUsername))
				{
					connectionString = connectionStringWithUsername;
				}

				WriteLegacyCacheWarningIfNeeded(connectionString);

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

			// Only the legacy tokens.json entry is per-profile. The SDK MSAL cache (issue #217) is shared by all
			// profiles and the signed-in account may still be used by other profiles of the same tenant, so it is
			// intentionally left untouched here.
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
			// Issue #217: OAuth profiles on systems with OS secure storage use the SDK MSAL cache, which is encrypted
			// and refreshes tokens. Everything else falls through to the pre-#217 code below, intentionally unchanged.
			var mode = TokenCacheModeSelector.Select(connectionString, this.secureTokenStorageProbe.IsAvailable);
			if (mode == TokenCacheMode.SdkSecureCache)
			{
				return await CreateServiceClientWithSdkTokenCacheAsync(connectionName, connectionString);
			}

			// Pre-#217 path, used for TokenCacheMode.NotApplicable (ClientSecret, Certificate, unparsable strings)
			// and TokenCacheMode.LegacyPacxCache (OAuth without secure storage). Do not change it without
			// re-validating those scenarios: it is the "no regression" guarantee of #217.
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


		/// <summary>
		/// Creates an OAuth <see cref="ServiceClient"/> that relies on the SDK MSAL token cache (issue #217).
		/// </summary>
		/// <remarks>
		/// <para>
		/// The SDK is given the connection string plus <c>TokenCacheStorePath</c> = the shared cache FILE. With
		/// <c>Username</c> and <c>LoginPrompt=Auto</c> in the profile, the SDK then: reuses the cached account
		/// silently in every new process, refreshes the access token with the cached refresh token, also in the
		/// middle of a long-running command, and asks for an interactive sign-in only when nothing usable is cached.
		/// All of this was verified at runtime with SDK 1.2.26 on 2026-10-18 (including expiry during a
		/// 90-minute run, 3 parallel processes, corrupt and inaccessible cache files).
		/// </para>
		/// <para>
		/// <c>tokens.json</c> is neither read nor written here: its tokens are not refreshable and are stored in
		/// plain text, which is exactly what #217 removes.
		/// </para>
		/// </remarks>
		private async Task<ServiceClient> CreateServiceClientWithSdkTokenCacheAsync(string connectionName, string connectionString)
		{
			var sdkConnectionString = OAuthConnectionString.WithTokenCachePath(connectionString, TokenCache.GetMsalCacheFilePath());

			ServiceClient crm;
			try
			{
				crm = new ServiceClient(sdkConnectionString);
			}
			catch (DataverseConnectionException ex)
			{
				output.WriteLine($"Failed to create ServiceClient for connection '{connectionName}': {ex.Message}", ConsoleColor.Yellow);
				RecourseInnerExceptions(ex, 1);
				throw;
			}

			if (crm.IsReady)
			{
				// Upgrades OAuth profiles created before #217: they have no Username, so without this every command
				// would ignore the cache and sign in again. After this first sign-in they reuse the cache silently.
				await PersistAuthenticatedUsernameAsync(connectionName, connectionString, crm.OAuthUserId);

				// The profile now works through the encrypted SDK cache: its plain-text token in tokens.json is no
				// longer needed and is removed (only after a successful connection, so a failure never loses it).
				await TryRetireLegacyTokenAsync(TokenCache.GetTokenCachePath(), connectionName);
			}

			return crm;
		}


		/// <summary>
		/// Removes the plain-text <c>tokens.json</c> entry of a profile that now uses the SDK MSAL cache (issue #217).
		/// </summary>
		/// <remarks>
		/// <para>
		/// BEST EFFORT on purpose: this is cleanup, not part of connecting. A locked file (another PACX process
		/// writing it), a permission problem or a corrupt file must never make a working connection fail, so such
		/// errors are swallowed and the entry is simply retried on the next connection of the profile.
		/// </para>
		/// <para>
		/// Two processes cleaning different profiles at the same time can lose one removal (read-modify-write on the
		/// same file). The effect is harmless: the remaining entry is never used for that profile again (SDK mode does
		/// not read tokens.json) and is removed on its next successful connection.
		/// </para>
		/// <para>
		/// A legacy token is never imported into the MSAL cache: it is only an access token, without the refresh
		/// token MSAL needs, so it could not provide silent renewal. Upgraded profiles sign in once instead.
		/// </para>
		/// </remarks>
		/// <returns><c>true</c> if the cleanup completed (including "nothing to remove"), <c>false</c> if it failed.</returns>
		internal static async Task<bool> TryRetireLegacyTokenAsync(string tokenCacheDirectory, string connectionName)
		{
			try
			{
				await TokenCache.ClearAccessTokenAsync(tokenCacheDirectory, connectionName);
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Newtonsoft.Json.JsonException)
			{
				return false;
			}
		}


		/// <summary>
		/// Warns the user, at profile creation, that this system cannot store tokens securely (issue #217).
		/// </summary>
		/// <remarks>
		/// In <see cref="TokenCacheMode.LegacyPacxCache"/> mode PACX keeps its pre-#217 behavior to avoid a
		/// regression (approved on 2026-10-18), but that behavior has two drawbacks the user must know about:
		/// the token is stored in plain text, and it cannot be refreshed. Shown only by <c>auth create</c>, where the
		/// user can still choose another authentication type, and not on every command, so that the output of other
		/// commands (often parsed by scripts) does not change.
		/// </remarks>
		/// <returns><c>true</c> if the warning has been written.</returns>
		/// <param name="connectionString">The connection string of the profile being created.</param>
		/// <param name="getTokenCacheDirectory">
		/// Returns the folder that holds <c>tokens.json</c>, shown in the message. Evaluated only when the warning is
		/// shown, because <see cref="TokenCache.GetTokenCachePath"/> creates folders under the user's real data folder;
		/// tests pass a temporary folder (review of PR #257).
		/// </param>
		internal bool WriteLegacyCacheWarningIfNeeded(string connectionString, Func<string>? getTokenCacheDirectory = null)
		{
			var mode = TokenCacheModeSelector.Select(connectionString, this.secureTokenStorageProbe.IsAvailable);
			if (mode != TokenCacheMode.LegacyPacxCache) return false;

			var tokenCacheDirectory = (getTokenCacheDirectory ?? TokenCache.GetTokenCachePath)();
			var legacyFile = Path.Combine(tokenCacheDirectory, TokenCache.LegacyTokenFileName);

			output.WriteLine();
			output.WriteLine("Warning: no secure token storage is available on this system (for example, no OS keyring on Linux).", ConsoleColor.Yellow);
			output.WriteLine($"PACX will reuse the access token stored in plain text (readable only by your user) in: {legacyFile}", ConsoleColor.Yellow);
			output.WriteLine("That token cannot be refreshed: commands running longer than its lifetime (about 1 hour) may fail.", ConsoleColor.Yellow);
			output.WriteLine("For unattended or headless use, prefer a ClientSecret or Certificate authentication profile.", ConsoleColor.Yellow);
			return true;
		}


		/// <summary>
		/// Stores the signed-in UPN into an existing OAuth profile that does not specify a user yet (issue #217).
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// <item>Never overwrites a user name the profile already contains: if it differs from the account that
		/// signed in, a warning is shown and the profile is left untouched, because the user chose that value.</item>
		/// <item>Only updates the profile if it still stores exactly the connection string used for this sign-in.
		/// This avoids overwriting a profile changed meanwhile (e.g. by another PACX process) and does nothing when
		/// the profile is not stored yet, as during <c>auth create</c>, which adds the user name itself.</item>
		/// <item>Non-OAuth profiles and sign-ins without a known user are ignored.</item>
		/// <item>
		/// The update is an atomic, cross-process read-modify-write (<see cref="IAtomicSettingsRepository.UpdateAsync"/>)
		/// on the settings READ FROM DISK, never on the in-memory snapshot (review of PR #257). The snapshot is loaded
		/// before the sign-in, which can take minutes; saving it would silently revert what another PACX process did
		/// meanwhile (a profile created or deleted, a different default). If the injected settings repository has no
		/// atomic update, the profile is left unchanged: losing the user name only costs one more sign-in later,
		/// while overwriting concurrent changes would lose user data.
		/// </item>
		/// <item>
		/// NEVER FAILS THE COMMAND (review of PR #257): this runs after the SDK client is already authenticated, and
		/// storing the user name is only an optimization for later commands. If the settings cannot be read or saved
		/// (I/O error, file locked beyond the retry, permissions, corrupt or undecryptable settings) a warning is shown
		/// and the authenticated client is still used; the next command simply signs in again and retries the save.
		/// </item>
		/// </list>
		/// </remarks>
		/// <param name="connectionName">Name of the profile used for the sign-in.</param>
		/// <param name="usedConnectionString">The connection string as stored in the profile and used to connect.</param>
		/// <param name="signedInUser">The UPN returned by the SDK (<see cref="ServiceClient.OAuthUserId"/>).</param>
		internal async Task PersistAuthenticatedUsernameAsync(string connectionName, string usedConnectionString, string? signedInUser)
		{
			if (string.IsNullOrWhiteSpace(signedInUser)) return;
			if (!OAuthConnectionString.IsOAuth(usedConnectionString)) return;

			var configuredUser = OAuthConnectionString.GetUsername(usedConnectionString);
			if (configuredUser != null)
			{
				if (!string.Equals(configuredUser, signedInUser.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					output.WriteLine($"Warning: authentication profile '{connectionName}' is configured for user '{configuredUser}', but '{signedInUser}' signed in. The profile has not been changed.", ConsoleColor.Yellow);
				}
				return;
			}

			if (!OAuthConnectionString.TryAddUsername(usedConnectionString, signedInUser, out var updatedConnectionString)) return;

			if (this.settings is not IAtomicSettingsRepository atomicSettings) return;

			ConnectionSetting persisted;
			try
			{
				persisted = await atomicSettings.UpdateAsync<ConnectionSetting>("connections", current =>
				{
					if (current == null) throw new KeepSettingsUnchangedException();

					// Same normalization as GetConnectionSettingAsync: stored strings are always encrypted.
					if (!current.IsSecured.GetValueOrDefault())
					{
						current.SecureSettings(GetAesKey(), GetAesIV());
					}

					if (!current.TryGetConnectionString(connectionName, GetAesKey(), GetAesIV(), out var storedConnectionString)
						|| !string.Equals(storedConnectionString, usedConnectionString, StringComparison.Ordinal))
					{
						throw new KeepSettingsUnchangedException();
					}

					current.UpsertConnectionString(connectionName, updatedConnectionString, GetAesKey(), GetAesIV());
					return current;
				});
			}
			catch (KeepSettingsUnchangedException)
			{
				// UpdateAsync keeps the persisted value when the callback throws: nothing has been written.
				return;
			}
			catch (Exception ex) when (ex is IOException
				or UnauthorizedAccessException
				or Newtonsoft.Json.JsonException
				or System.Security.Cryptography.CryptographicException
				or FormatException) // malformed Base64 ciphertext in the stored profile (AesEncryption.Decrypt)
			{
				output.WriteLine($"Warning: could not save the signed-in user into authentication profile '{connectionName}': {ex.Message}. The next command may ask you to sign in again.", ConsoleColor.Yellow);
				return;
			}

			// The fresh settings become the in-memory copy, so later saves in this process do not reuse the stale one.
			this.cache = persisted;
		}


		/// <summary>
		/// Thrown inside an <see cref="IAtomicSettingsRepository.UpdateAsync"/> callback to leave the settings
		/// exactly as they are on disk: the contract of UpdateAsync preserves the persisted value and releases the lock.
		/// </summary>
		private sealed class KeepSettingsUnchangedException : Exception
		{
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
