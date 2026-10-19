namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Issue #217: the token cache mode is chosen by capability (is secure storage usable?) and not by OS.
	/// </summary>
	[TestClass]
	public class TokenCacheModeSelectorTest
	{
		private const string OAuthProfile = "AuthType=OAuth;Url=https://org.crm4.dynamics.com;RedirectUri=http://localhost;LoginPrompt=Auto";
		private const string ClientSecretProfile = "AuthType=ClientSecret;Url=https://org.crm4.dynamics.com;ClientId=id;ClientSecret=secret";
		private const string CertificateProfile = "AuthType=Certificate;Url=https://org.crm4.dynamics.com;ClientId=id;Thumbprint=ABC";

		[TestMethod]
		public void Select_OAuthWithSecureStorage_ShouldUseTheSdkSecureCache()
		{
			Assert.AreEqual(TokenCacheMode.SdkSecureCache, TokenCacheModeSelector.Select(OAuthProfile, () => true));
		}

		[TestMethod]
		public void Select_OAuthWithoutSecureStorage_ShouldFallBackToTheLegacyCache()
		{
			Assert.AreEqual(TokenCacheMode.LegacyPacxCache, TokenCacheModeSelector.Select(OAuthProfile, () => false));
		}

		[TestMethod]
		[DataRow(ClientSecretProfile)]
		[DataRow(CertificateProfile)]
		[DataRow("AuthType=OAuth;Url='unterminated")]
		[DataRow("")]
		[DataRow(null)]
		public void Select_NonOAuthOrUnparsable_ShouldKeepThePreviousCodePathWithoutProbing(string? connectionString)
		{
			var probed = false;

			var mode = TokenCacheModeSelector.Select(connectionString, () => { probed = true; return true; });

			Assert.AreEqual(TokenCacheMode.NotApplicable, mode);
			Assert.IsFalse(probed, "Probing may show a keyring unlock prompt on Linux: it must never happen for non-OAuth profiles.");
		}

		[TestMethod]
		public void Select_WithNullProbe_ShouldThrow()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => TokenCacheModeSelector.Select(OAuthProfile, null!));
		}

		[TestMethod]
		public void BuildMsalCacheFilePath_ShouldPointToAFileInsideTheCacheDirectory()
		{
			var directory = Path.Combine(Path.GetTempPath(), "pacx-tests", "tokenCache");

			var path = TokenCache.BuildMsalCacheFilePath(directory);

			Assert.AreEqual(directory, Path.GetDirectoryName(path), "The SDK splits the value into directory + file: it must be a file inside the cache directory.");
			Assert.AreEqual(TokenCache.MsalCacheFileName, Path.GetFileName(path));
		}

		[TestMethod]
		public void MsalSecureTokenStorageProbe_ShouldNotResolveTheDirectoryUntilAsked()
		{
			var resolved = false;

			_ = new MsalSecureTokenStorageProbe(() => { resolved = true; return Path.GetTempPath(); });

			Assert.IsFalse(resolved, "Commands that do not connect to Dataverse must not create folders or probe the OS storage.");
		}
	}
}
