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
			await SaveAsync(GetTokenCachePath());
		}

		internal async Task SaveAsync(string tokenCacheDirectory)
		{
			var tokenCacheFile = Path.Combine(tokenCacheDirectory, LegacyTokenFileName);
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			await WriteOwnerOnlyAsync(tokenCacheFile, json);
		}


		/// <summary>
		/// File name of the legacy plain-text token cache.
		/// </summary>
		internal const string LegacyTokenFileName = "tokens.json";


		/// <summary>
		/// Writes <paramref name="content"/> so that, on Linux and macOS, only the current user can read the file.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Issue #217: <c>tokens.json</c> contains bearer tokens in PLAIN TEXT. It is still written in
		/// <see cref="TokenCacheMode.LegacyPacxCache"/> mode (no OS secure storage, typically headless Linux),
		/// approved to avoid a regression there. Restricting the file to the owner (<c>0600</c>) is the mitigation
		/// agreed for that mode: other local users cannot read the token.
		/// </para>
		/// <para>
		/// The mode is applied BEFORE the content is written, both to a new file (<c>UnixCreateMode</c>) and to an
		/// existing one created by an older PACX with the default umask, so the token is never readable by others.
		/// On Windows the file is written exactly as before #217: it lives in the user's own LocalAppData, whose
		/// default ACL already restricts it to the user, and keeping the code identical avoids any regression.
		/// </para>
		/// </remarks>
		internal static async Task WriteOwnerOnlyAsync(string path, string content)
		{
			if (OperatingSystem.IsWindows())
			{
				await File.WriteAllTextAsync(path, content);
				return;
			}

			const UnixFileMode ownerReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;

			if (File.Exists(path))
			{
				File.SetUnixFileMode(path, ownerReadWrite);
			}

			var options = new FileStreamOptions
			{
				Mode = FileMode.Create,
				Access = FileAccess.Write,
				Share = FileShare.None,
				UnixCreateMode = ownerReadWrite,
			};

			await using var stream = new FileStream(path, options);
			await using var writer = new StreamWriter(stream);
			await writer.WriteAsync(content);
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


		private static async Task<TokenCache> GetOrCreateAsync(string tokenCacheDirectory)
		{
			var tokenCacheFile = Path.Combine(tokenCacheDirectory, LegacyTokenFileName);
			if (!File.Exists(tokenCacheFile)) return new TokenCache();

			RestrictExistingFileToOwner(tokenCacheFile);

			var json = await File.ReadAllTextAsync(tokenCacheFile);
			return JsonConvert.DeserializeObject<TokenCache>(json) ?? [];
		}


		/// <summary>
		/// On Linux and macOS, removes group and other permissions from an existing <c>tokens.json</c>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Issue #217, review of PR #257: releases before #217 created <c>tokens.json</c> with the default umask
		/// (typically <c>0644</c>, readable by every local user). <see cref="WriteOwnerOnlyAsync"/> only fixes that
		/// when the file is written, but PACX often just READS it: in legacy mode a still-valid token is reused
		/// without rewriting the file, and with secure storage the entries of other profiles stay untouched. So the
		/// permissions are also tightened on every read, BEFORE the content is read.
		/// </para>
		/// <para>
		/// Owner bits are kept as they are; only group/other bits are removed. Best effort: if the mode cannot be
		/// changed (e.g. the file belongs to another user) the read proceeds as before #217 instead of failing.
		/// Nothing to do on Windows: the file lives in the user's own LocalAppData, restricted by its default ACL.
		/// </para>
		/// </remarks>
		internal static void RestrictExistingFileToOwner(string path)
		{
			if (OperatingSystem.IsWindows()) return;

			const UnixFileMode groupAndOther =
				UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
				UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

			try
			{
				var mode = File.GetUnixFileMode(path);
				if ((mode & groupAndOther) != 0)
				{
					File.SetUnixFileMode(path, mode & ~groupAndOther);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Best effort, see remarks.
			}
		}


		public static Task<TokenDefinition?> TryGetAccessTokenAsync(string connectionName)
		{
			return TryGetAccessTokenAsync(GetTokenCachePath(), connectionName);
		}

		internal static async Task<TokenDefinition?> TryGetAccessTokenAsync(string tokenCacheDirectory, string connectionName)
		{
			var tokenCache = await GetOrCreateAsync(tokenCacheDirectory);
			if (!tokenCache.TryGetValue(connectionName, out var accessToken)) return null;
			if (accessToken == null || !accessToken.IsValid) return null;
			return accessToken;
		}


		public static Task SaveAccessTokenAsync(string name, Uri serviceUri, string currentAccessToken)
		{
			return SaveAccessTokenAsync(GetTokenCachePath(), name, serviceUri, currentAccessToken);
		}

		internal static async Task SaveAccessTokenAsync(string tokenCacheDirectory, string name, Uri serviceUri, string currentAccessToken)
		{
			var tokenCache = await GetOrCreateAsync(tokenCacheDirectory);
			tokenCache.Set(name, serviceUri, currentAccessToken);

			await tokenCache.SaveAsync(tokenCacheDirectory);
		}

		public static Task ClearAccessTokenAsync(string connectionName)
		{
			return ClearAccessTokenAsync(GetTokenCachePath(), connectionName);
		}

		/// <summary>
		/// Removes the legacy token of one profile. Does not create or rewrite the file when there is nothing to remove.
		/// </summary>
		internal static async Task ClearAccessTokenAsync(string tokenCacheDirectory, string connectionName)
		{
			var tokenCache = await GetOrCreateAsync(tokenCacheDirectory);
			if (!tokenCache.ContainsKey(connectionName)) return;

			tokenCache.Remove(connectionName);
			await tokenCache.SaveAsync(tokenCacheDirectory);
		}
	}
}
