using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// PACX legacy token cache (<c>tokens.json</c>) plus the location of the SDK MSAL cache file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>tokens.json</c> stores the last ACCESS token of each OAuth profile in PLAIN TEXT so that the next PACX
	/// process can reuse it without a new sign-in. It cannot be refreshed (no refresh token is stored), which is
	/// the root cause of issue #217: a command that outlives that token fails with an unauthorized error.
	/// </para>
	/// <para>
	/// Since #217 it is used ONLY in <see cref="TokenCacheMode.LegacyPacxCache"/> mode, i.e. for OAuth profiles on
	/// systems without usable secure storage. Everywhere else the SDK MSAL cache (<see cref="GetMsalCacheFilePath"/>)
	/// is used instead: encrypted at rest and refreshable.
	/// </para>
	/// </remarks>
	public class TokenCache : Dictionary<string, TokenDefinition>
	{
		/// <summary>
		/// File name of the SDK MSAL token cache inside <see cref="GetTokenCachePath"/>.
		/// </summary>
		/// <remarks>
		/// ONE file shared by all profiles, on purpose (decision of 2026-10-18):
		/// MSAL stores several accounts in the same cache and the profile <c>Username</c> selects the right one;
		/// profiles of the same tenant share a single sign-in; and on Linux the keyring entry is keyed by fixed
		/// SDK attributes rather than by this file name, so per-profile files would not isolate anything there.
		/// </remarks>
		public const string MsalCacheFileName = "msal_token_cache.bin";


		/// <summary>
		/// Returns the full path of the SDK MSAL token cache FILE.
		/// </summary>
		/// <remarks>
		/// The SDK requires a file path for <c>TokenCacheStorePath</c>. Before #217 PACX passed the directory
		/// returned by <see cref="GetTokenCachePath"/>, so the SDK cache was never written.
		/// On Windows the file is DPAPI-encrypted for the current user. On Linux it stays EMPTY: the SDK keeps
		/// the tokens in the keyring and only uses the file as a marker.
		/// </remarks>
		public static string GetMsalCacheFilePath()
		{
			return BuildMsalCacheFilePath(GetTokenCachePath());
		}

		internal static string BuildMsalCacheFilePath(string tokenCacheDirectory)
		{
			return Path.Combine(tokenCacheDirectory, MsalCacheFileName);
		}


		public void Set(string name, Uri serviceUri, string token)
		{
			this[name] = new TokenDefinition(serviceUri, token);
		}


		public async Task SaveAsync()
		{
			var tokenCachePath = GetTokenCachePath();
			var tokenCacheFile = Path.Combine(tokenCachePath, "tokens.json");
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			await File.WriteAllTextAsync(tokenCacheFile, json);
		}



		public static string GetTokenCachePath()
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


		private static async Task<TokenCache> GetOrCreateAsync()
		{
			var tokenCachePath = GetTokenCachePath();
			var tokenCacheFile = Path.Combine(tokenCachePath, "tokens.json");
			if (!File.Exists(tokenCacheFile)) return new TokenCache();
			var json = await File.ReadAllTextAsync(tokenCacheFile);
			return JsonConvert.DeserializeObject<TokenCache>(json) ?? [];
		}


		public static async Task<TokenDefinition?> TryGetAccessTokenAsync(string connectionName)
		{
			var tokenCache = await GetOrCreateAsync();
			if (!tokenCache.TryGetValue(connectionName, out var accessToken)) return null;
			if (accessToken == null || !accessToken.IsValid) return null;
			return accessToken;
		}


		public static async Task SaveAccessTokenAsync(string name, Uri serviceUri, string currentAccessToken)
		{
			var tokenCache = await GetOrCreateAsync();
			tokenCache.Set(name, serviceUri, currentAccessToken);

			await tokenCache.SaveAsync();
		}

		public static async Task ClearAccessTokenAsync(string connectionName)
		{
			var tokenCache = await GetOrCreateAsync();
			if (!tokenCache.ContainsKey(connectionName)) return;

			tokenCache.Remove(connectionName);
			await tokenCache.SaveAsync();
		}
	}
}
