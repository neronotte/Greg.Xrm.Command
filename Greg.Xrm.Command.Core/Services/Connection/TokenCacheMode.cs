namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// How PACX keeps tokens between commands for a given authentication profile (issue #217).
	/// </summary>
	public enum TokenCacheMode
	{
		/// <summary>
		/// Non-interactive profiles (ClientSecret, Certificate) and connection strings PACX cannot parse.
		/// They keep the exact pre-#217 code path: the SDK re-acquires and refreshes confidential-client tokens
		/// by itself, so issue #217 does not affect them and changing their path would only add risk.
		/// </summary>
		NotApplicable,

		/// <summary>
		/// OAuth profile on a system where the OS secure storage works (DPAPI on Windows, keyring on Linux,
		/// Keychain on macOS). The SDK MSAL cache is used: tokens are encrypted at rest, reused across processes
		/// and refreshed by the SDK, including in the middle of a long-running command. This is the fix for #217.
		/// </summary>
		SdkSecureCache,

		/// <summary>
		/// OAuth profile on a system WITHOUT usable secure storage (headless Linux, WSL without a keyring,
		/// a locked or refused keyring). There the SDK keeps tokens in memory only, so every new PACX process
		/// would require a new interactive sign-in. To avoid that regression PACX keeps its pre-#217 behavior:
		/// it reuses the last access token stored in plain text in <c>tokens.json</c>. The #217 limitation
		/// remains in this mode (that token cannot be refreshed). Approved by the maintainer on 2026-10-18.
		/// </summary>
		LegacyPacxCache
	}


	/// <summary>
	/// Chooses the <see cref="TokenCacheMode"/> for a connection string.
	/// </summary>
	/// <remarks>
	/// The decision is based on the CAPABILITY of the system (is secure storage usable?) and not on the OS name:
	/// Linux with a working keyring gets the secure SDK cache like Windows, and only systems where it cannot work
	/// fall back to the legacy cache. Kept as a pure function so that every combination is unit tested.
	/// </remarks>
	public static class TokenCacheModeSelector
	{
		/// <summary>
		/// Selects the token cache mode.
		/// </summary>
		/// <param name="connectionString">The stored connection string of the profile.</param>
		/// <param name="isSecureStorageAvailable">
		/// Evaluated ONLY for OAuth profiles. This matters on desktop Linux: probing a locked keyring makes the
		/// OS show an unlock prompt, which must never appear for ClientSecret/Certificate profiles.
		/// </param>
		public static TokenCacheMode Select(string? connectionString, Func<bool> isSecureStorageAvailable)
		{
			ArgumentNullException.ThrowIfNull(isSecureStorageAvailable);

			if (!OAuthConnectionString.IsOAuth(connectionString))
				return TokenCacheMode.NotApplicable;

			return isSecureStorageAvailable()
				? TokenCacheMode.SdkSecureCache
				: TokenCacheMode.LegacyPacxCache;
		}
	}
}
