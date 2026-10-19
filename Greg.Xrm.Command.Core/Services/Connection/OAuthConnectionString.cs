using System.Data.Common;
using System.Text;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Inspects and safely extends Dataverse connection strings for the interactive OAuth flow
	/// (<c>AuthType=OAuth</c>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// WHY THIS CLASS EXISTS (issue #217, see docs/issue-217-auth-token-implementation-plan.md).
	/// The Dataverse SDK (Microsoft.PowerPlatform.Dataverse.Client 1.2.26) can persist the MSAL token cache,
	/// reuse it across PACX processes and refresh expired tokens by itself, but ONLY if two connection string
	/// values are right. Both were verified at runtime on 2026-10-18:
	/// </para>
	/// <list type="number">
	/// <item>
	/// <c>TokenCacheStorePath</c> must be a full FILE path. The SDK splits the value into directory + file name.
	/// PACX historically passed its <c>tokenCache</c> DIRECTORY, so the SDK tried to write a file with the same
	/// name as an existing directory and silently persisted nothing.
	/// </item>
	/// <item>
	/// <c>Username</c> must contain the signed-in UPN. On every new process the SDK looks up the cached account
	/// by this value (AuthProcessor.TryGetAccountFromCache). If it is missing, the lookup never matches, the
	/// cache is ignored and the SDK falls back to an interactive (browser) sign-in on every command, even when a
	/// valid refresh token is cached. Users never type their UPN in <c>pacx auth create --environment</c>, so
	/// PACX captures it after the first successful sign-in and stores it in the profile.
	/// </item>
	/// </list>
	/// <para>
	/// DESIGN RULE: values are only ever APPENDED to the original text, which is never parsed-and-rewritten.
	/// Whatever the user stored (including keys this class does not know, their order and spelling) reaches
	/// the SDK unchanged. Existing values always win: an explicit <c>TokenCacheStorePath</c> or <c>Username</c>
	/// typed by the user is never overwritten.
	/// </para>
	/// <para>
	/// Parsing uses <see cref="DbConnectionStringBuilder"/>, the same key=value grammar used by the SDK, so key
	/// names are case-insensitive and quoted values are handled. If a string cannot be parsed, every method
	/// behaves as if it were not an OAuth connection string and returns it unchanged: PACX then follows its
	/// pre-#217 code path, which is the safest choice for input we do not understand.
	/// </para>
	/// </remarks>
	public static class OAuthConnectionString
	{
		/// <summary>
		/// Aliases accepted by the SDK for the authentication type key.
		/// </summary>
		internal static readonly string[] AuthTypeKeys = ["AuthType", "AuthenticationType"];

		/// <summary>
		/// Aliases accepted by the SDK for the user name key. The SDK uses this value as the MSAL login hint
		/// and to find the cached account. Key comparison is case-insensitive, so "Username" also covers "UserName".
		/// </summary>
		internal static readonly string[] UsernameKeys = ["Username", "User Name", "UserId", "User Id"];

		/// <summary>
		/// Key used by the SDK for the MSAL token cache location. The SDK accepts no alias for it.
		/// </summary>
		internal const string TokenCacheStorePathKey = "TokenCacheStorePath";

		/// <summary>
		/// Key written by PACX when it stores the signed-in UPN. Any alias in <see cref="UsernameKeys"/> works for the SDK.
		/// </summary>
		internal const string UsernameKeyToWrite = "Username";


		/// <summary>
		/// Returns <c>true</c> when the connection string uses the interactive OAuth flow (<c>AuthType=OAuth</c>).
		/// </summary>
		/// <remarks>
		/// Only this flow is affected by issue #217. ClientSecret and Certificate profiles use a confidential client
		/// whose tokens the SDK already refreshes in memory and that cannot use a user token cache, so PACX leaves
		/// them on their existing code path.
		/// </remarks>
		/// <param name="connectionString">The connection string to inspect.</param>
		public static bool IsOAuth(string? connectionString)
		{
			var builder = TryParse(connectionString);
			if (builder == null) return false;

			var authType = GetFirstValue(builder, AuthTypeKeys);
			return string.Equals(authType, "OAuth", StringComparison.OrdinalIgnoreCase);
		}


		/// <summary>
		/// Returns the user name stored in the connection string (any alias accepted by the SDK),
		/// or <c>null</c> if none is present or the string cannot be parsed.
		/// </summary>
		/// <param name="connectionString">The connection string to inspect.</param>
		public static string? GetUsername(string? connectionString)
		{
			var builder = TryParse(connectionString);
			if (builder == null) return null;

			return GetFirstValue(builder, UsernameKeys);
		}


		/// <summary>
		/// For OAuth connection strings without an explicit <c>TokenCacheStorePath</c>, appends one pointing to
		/// <paramref name="cacheFilePath"/>. Any other connection string is returned unchanged.
		/// </summary>
		/// <remarks>
		/// An explicit user value is preserved on purpose: a user may deliberately keep a separate cache.
		/// </remarks>
		/// <param name="connectionString">The stored connection string.</param>
		/// <param name="cacheFilePath">Full path of the MSAL cache FILE (not a directory).</param>
		/// <returns>The connection string to hand to the SDK.</returns>
		public static string WithTokenCachePath(string connectionString, string cacheFilePath)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
			ArgumentException.ThrowIfNullOrWhiteSpace(cacheFilePath);

			var builder = TryParse(connectionString);
			if (builder == null || !IsOAuth(connectionString)) return connectionString;
			if (GetFirstValue(builder, [TokenCacheStorePathKey]) != null) return connectionString;

			return Append(connectionString, TokenCacheStorePathKey, cacheFilePath);
		}


		/// <summary>
		/// For OAuth connection strings without any user name, appends <c>Username=<paramref name="username"/></c>.
		/// </summary>
		/// <remarks>
		/// A user name that is already present is never replaced, even if it differs from the account that actually
		/// signed in: changing what the user configured is a decision for the user, not for PACX.
		/// </remarks>
		/// <param name="connectionString">The stored connection string.</param>
		/// <param name="username">The UPN of the account that signed in (ServiceClient.OAuthUserId).</param>
		/// <param name="updatedConnectionString">
		/// The connection string with the user name appended, or the original string when nothing was added.
		/// </param>
		/// <returns><c>true</c> if the user name has been appended, <c>false</c> otherwise.</returns>
		public static bool TryAddUsername(string connectionString, string? username, out string updatedConnectionString)
		{
			updatedConnectionString = connectionString;

			if (string.IsNullOrWhiteSpace(connectionString)) return false;
			if (string.IsNullOrWhiteSpace(username)) return false;
			if (!IsOAuth(connectionString)) return false;
			if (GetUsername(connectionString) != null) return false;

			updatedConnectionString = Append(connectionString, UsernameKeyToWrite, username.Trim());
			return true;
		}



		private static DbConnectionStringBuilder? TryParse(string? connectionString)
		{
			if (string.IsNullOrWhiteSpace(connectionString)) return null;

			try
			{
				return new DbConnectionStringBuilder { ConnectionString = connectionString };
			}
			catch (ArgumentException)
			{
				// Malformed string: callers treat it as "not OAuth" and keep the pre-#217 behavior.
				return null;
			}
		}

		private static string? GetFirstValue(DbConnectionStringBuilder builder, IEnumerable<string> keys)
		{
			foreach (var key in keys)
			{
				if (builder.TryGetValue(key, out var value)
					&& value is string text
					&& !string.IsNullOrWhiteSpace(text))
				{
					return text.Trim();
				}
			}
			return null;
		}

		private static string Append(string connectionString, string key, string value)
		{
			// AppendKeyValuePair adds the ';' separator when needed and quotes the value if it contains
			// characters such as ';' or quotes, exactly as the SDK parser expects to read it back.
			var builder = new StringBuilder(connectionString.TrimEnd());
			DbConnectionStringBuilder.AppendKeyValuePair(builder, key, value);
			return builder.ToString();
		}
	}
}
