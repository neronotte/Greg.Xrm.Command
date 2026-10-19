namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Issue #217: the connection string must carry a FILE <c>TokenCacheStorePath</c> and the signed-in
	/// <c>Username</c> for the SDK to persist and reuse its MSAL cache. These tests pin the rules used to add them.
	/// </summary>
	[TestClass]
	public class OAuthConnectionStringTest
	{
		private const string OAuthProfile = "AuthType=OAuth;Url=https://org.crm4.dynamics.com;RedirectUri=http://localhost;LoginPrompt=Auto";
		private const string ClientSecretProfile = "AuthType=ClientSecret;Url=https://org.crm4.dynamics.com;ClientId=00000000-0000-0000-0000-000000000001;ClientSecret=secret";
		private const string CertificateProfile = "AuthType=Certificate;Url=https://org.crm4.dynamics.com;ClientId=00000000-0000-0000-0000-000000000001;Thumbprint=ABC";
		private const string CacheFile = @"C:\Users\me\AppData\Local\Greg.Xrm.Command\tokenCache\msal_token_cache.bin";


		// ── IsOAuth ───────────────────────────────────────────────────────────

		[TestMethod]
		[DataRow("AuthType=OAuth;Url=https://org.crm.dynamics.com")]
		[DataRow("authtype=oauth;Url=https://org.crm.dynamics.com")]
		[DataRow("AuthenticationType=OAuth;Url=https://org.crm.dynamics.com")]
		[DataRow("Url=https://org.crm.dynamics.com; AuthType = OAuth ")]
		public void IsOAuth_WithOAuthAuthType_ShouldReturnTrue(string connectionString)
		{
			Assert.IsTrue(OAuthConnectionString.IsOAuth(connectionString));
		}

		[TestMethod]
		[DataRow(ClientSecretProfile)]
		[DataRow(CertificateProfile)]
		[DataRow("Url=https://org.crm.dynamics.com")]
		[DataRow("")]
		[DataRow("   ")]
		[DataRow(null)]
		[DataRow("AuthType=OAuth;Url='unterminated")]
		public void IsOAuth_WithNonOAuthEmptyOrMalformedString_ShouldReturnFalse(string? connectionString)
		{
			Assert.IsFalse(OAuthConnectionString.IsOAuth(connectionString));
		}


		// ── WithTokenCachePath ────────────────────────────────────────────────

		[TestMethod]
		public void WithTokenCachePath_WhenMissing_ShouldAppendFilePathAndKeepOriginalText()
		{
			var result = OAuthConnectionString.WithTokenCachePath(OAuthProfile, CacheFile);

			Assert.IsTrue(result.StartsWith(OAuthProfile, StringComparison.Ordinal), "The original text must reach the SDK unchanged.");
			Assert.AreEqual(OAuthProfile + ";TokenCacheStorePath=" + CacheFile, result);
		}

		[TestMethod]
		public void WithTokenCachePath_WhenOriginalEndsWithSeparator_ShouldNotDuplicateIt()
		{
			var result = OAuthConnectionString.WithTokenCachePath(OAuthProfile + ";", CacheFile);

			Assert.AreEqual(OAuthProfile + ";TokenCacheStorePath=" + CacheFile, result);
		}

		[TestMethod]
		[DataRow("TokenCacheStorePath")]
		[DataRow("tokencachestorepath")]
		public void WithTokenCachePath_WhenUserProvidedOne_ShouldKeepItUnchanged(string key)
		{
			var connectionString = OAuthProfile + ";" + key + @"=D:\custom\cache.dat";

			var result = OAuthConnectionString.WithTokenCachePath(connectionString, CacheFile);

			Assert.AreEqual(connectionString, result);
		}

		[TestMethod]
		[DataRow(ClientSecretProfile)]
		[DataRow(CertificateProfile)]
		public void WithTokenCachePath_ForNonOAuthProfiles_ShouldReturnStringUnchanged(string connectionString)
		{
			Assert.AreEqual(connectionString, OAuthConnectionString.WithTokenCachePath(connectionString, CacheFile));
		}

		[TestMethod]
		public void WithTokenCachePath_ForMalformedString_ShouldReturnStringUnchanged()
		{
			const string malformed = "AuthType=OAuth;Url='unterminated";

			Assert.AreEqual(malformed, OAuthConnectionString.WithTokenCachePath(malformed, CacheFile));
		}

		[TestMethod]
		public void WithTokenCachePath_WhenPathContainsSeparator_ShouldQuoteItSoTheSdkReadsItBack()
		{
			const string pathWithSeparator = @"C:\odd;folder\msal_token_cache.bin";

			var result = OAuthConnectionString.WithTokenCachePath(OAuthProfile, pathWithSeparator);

			var parsed = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = result };
			Assert.AreEqual(pathWithSeparator, parsed["TokenCacheStorePath"]);
			Assert.IsTrue(OAuthConnectionString.IsOAuth(result));
		}


		// ── GetUsername / TryAddUsername ─────────────────────────────────────

		[TestMethod]
		[DataRow("Username")]
		[DataRow("UserName")]
		[DataRow("User Name")]
		[DataRow("UserId")]
		[DataRow("User Id")]
		public void GetUsername_ShouldRecognizeEveryAliasAcceptedByTheSdk(string key)
		{
			var connectionString = OAuthProfile + ";" + key + "=user@contoso.com";

			Assert.AreEqual("user@contoso.com", OAuthConnectionString.GetUsername(connectionString));
		}

		[TestMethod]
		public void GetUsername_WhenMissing_ShouldReturnNull()
		{
			Assert.IsNull(OAuthConnectionString.GetUsername(OAuthProfile));
		}

		[TestMethod]
		public void TryAddUsername_WhenMissing_ShouldAppendItAndKeepOriginalText()
		{
			var added = OAuthConnectionString.TryAddUsername(OAuthProfile, "user@contoso.com", out var result);

			Assert.IsTrue(added);
			Assert.AreEqual(OAuthProfile + ";Username=user@contoso.com", result);
			Assert.AreEqual("user@contoso.com", OAuthConnectionString.GetUsername(result));
		}

		[TestMethod]
		[DataRow("Username")]
		[DataRow("User Name")]
		[DataRow("UserId")]
		[DataRow("User Id")]
		public void TryAddUsername_WhenAnyAliasIsPresent_ShouldNeverOverwriteIt(string key)
		{
			var connectionString = OAuthProfile + ";" + key + "=configured@contoso.com";

			var added = OAuthConnectionString.TryAddUsername(connectionString, "signedin@contoso.com", out var result);

			Assert.IsFalse(added);
			Assert.AreEqual(connectionString, result);
		}

		[TestMethod]
		[DataRow(ClientSecretProfile)]
		[DataRow(CertificateProfile)]
		public void TryAddUsername_ForNonOAuthProfiles_ShouldDoNothing(string connectionString)
		{
			var added = OAuthConnectionString.TryAddUsername(connectionString, "user@contoso.com", out var result);

			Assert.IsFalse(added);
			Assert.AreEqual(connectionString, result);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("   ")]
		public void TryAddUsername_WithoutSignedInUser_ShouldDoNothing(string? username)
		{
			var added = OAuthConnectionString.TryAddUsername(OAuthProfile, username, out var result);

			Assert.IsFalse(added);
			Assert.AreEqual(OAuthProfile, result);
		}

		[TestMethod]
		public void TokenCachePathAndUsername_CombinedAsAtRuntime_ShouldBothBeReadableByTheParser()
		{
			OAuthConnectionString.TryAddUsername(OAuthProfile, "user@contoso.com", out var stored);
			var runtime = OAuthConnectionString.WithTokenCachePath(stored, CacheFile);

			var parsed = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = runtime };
			Assert.AreEqual("user@contoso.com", parsed["Username"]);
			Assert.AreEqual(CacheFile, parsed["TokenCacheStorePath"]);
			Assert.AreEqual("Auto", parsed["LoginPrompt"], "LoginPrompt=Auto is required: with Never the SDK 1.2.26 never reads the cache.");
		}
	}
}
