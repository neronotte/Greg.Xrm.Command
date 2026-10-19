using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Issue #217, legacy cache retirement: tokens.json is only kept for systems without secure storage,
	/// is restricted to the owner on Unix, and its entries are removed once a profile uses the SDK cache.
	/// All tests use a temporary directory: the real %LOCALAPPDATA% cache is never touched.
	/// </summary>
	[TestClass]
	public class TokenCacheLegacyTest
	{
		private static readonly Uri ServiceUri = new("https://dev.crm4.dynamics.com");
		private string directory = null!;

		[TestInitialize]
		public void Initialize()
		{
			this.directory = Path.Combine(Path.GetTempPath(), "pacx-tests-issue217", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(this.directory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(this.directory)) Directory.Delete(this.directory, recursive: true);
		}

		private string LegacyFile => Path.Combine(this.directory, TokenCache.LegacyTokenFileName);


		// ── tokens.json behavior kept for LegacyPacxCache mode ────────────────

		[TestMethod]
		public async Task SaveAndRead_ShouldRoundTripTheTokenOfAProfile()
		{
			await TokenCache.SaveAccessTokenAsync(this.directory, "Dev", ServiceUri, "token-dev");

			var token = await TokenCache.TryGetAccessTokenAsync(this.directory, "Dev");

			Assert.IsNotNull(token);
			Assert.AreEqual("token-dev", token.AccessToken);
			Assert.AreEqual(ServiceUri, token.ServiceUri);
		}

		[TestMethod]
		public async Task Clear_ShouldRemoveOnlyTheGivenProfile()
		{
			await TokenCache.SaveAccessTokenAsync(this.directory, "Dev", ServiceUri, "token-dev");
			await TokenCache.SaveAccessTokenAsync(this.directory, "Uat", ServiceUri, "token-uat");

			await TokenCache.ClearAccessTokenAsync(this.directory, "Dev");

			Assert.IsNull(await TokenCache.TryGetAccessTokenAsync(this.directory, "Dev"));
			Assert.AreEqual("token-uat", (await TokenCache.TryGetAccessTokenAsync(this.directory, "Uat"))?.AccessToken);
		}

		[TestMethod]
		public async Task Clear_WhenNothingToRemove_ShouldNotCreateTheFile()
		{
			await TokenCache.ClearAccessTokenAsync(this.directory, "Dev");

			Assert.IsFalse(File.Exists(this.LegacyFile), "Retiring must not create a plain-text token file where none existed.");
		}

		[TestMethod]
		public async Task Clear_WhenProfileIsNotPresent_ShouldNotRewriteTheFile()
		{
			await TokenCache.SaveAccessTokenAsync(this.directory, "Uat", ServiceUri, "token-uat");
			var before = File.GetLastWriteTimeUtc(this.LegacyFile);
			var content = await File.ReadAllTextAsync(this.LegacyFile);

			await TokenCache.ClearAccessTokenAsync(this.directory, "Dev");

			Assert.AreEqual(before, File.GetLastWriteTimeUtc(this.LegacyFile));
			Assert.AreEqual(content, await File.ReadAllTextAsync(this.LegacyFile));
		}


		// ── Owner-only permissions ───────────────────────────────────────────

		[TestMethod]
		public async Task WriteOwnerOnly_ShouldWriteTheContent()
		{
			await TokenCache.WriteOwnerOnlyAsync(this.LegacyFile, "{ \"a\": 1 }");

			Assert.AreEqual("{ \"a\": 1 }", await File.ReadAllTextAsync(this.LegacyFile));
		}

		[TestMethod]
		public async Task WriteOwnerOnly_OnUnix_ShouldRestrictNewAndExistingFilesToTheOwner()
		{
			if (OperatingSystem.IsWindows())
			{
				// Windows keeps the pre-#217 write path; the file lives in the user's own LocalAppData.
				return;
			}

			const UnixFileMode ownerReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;

			await TokenCache.WriteOwnerOnlyAsync(this.LegacyFile, "new");
			Assert.AreEqual(ownerReadWrite, File.GetUnixFileMode(this.LegacyFile));

			// A file created by an older PACX with the default umask (e.g. 0644) must be tightened too.
			File.SetUnixFileMode(this.LegacyFile, ownerReadWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
			await TokenCache.WriteOwnerOnlyAsync(this.LegacyFile, "existing");
			Assert.AreEqual(ownerReadWrite, File.GetUnixFileMode(this.LegacyFile));
			Assert.AreEqual("existing", await File.ReadAllTextAsync(this.LegacyFile));
		}

		[TestMethod]
		public async Task Read_OnUnix_ShouldRestrictAPermissiveFileWithoutRewritingIt()
		{
			if (OperatingSystem.IsWindows())
			{
				// No group/other mode bits on Windows; the file lives in the user's own LocalAppData.
				return;
			}

			// File left by a release before #217 (default umask) with a still-valid token: PR #257 review.
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, TokenDefinition> { ["Dev"] = new(ServiceUri, "token-dev") });
			await File.WriteAllTextAsync(this.LegacyFile, json);
			File.SetUnixFileMode(this.LegacyFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
			var lastWrite = File.GetLastWriteTimeUtc(this.LegacyFile);

			var token = await TokenCache.TryGetAccessTokenAsync(this.directory, "Dev");

			Assert.AreEqual("token-dev", token?.AccessToken);
			Assert.AreEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(this.LegacyFile), "Reading alone must tighten the permissions.");
			Assert.AreEqual(lastWrite, File.GetLastWriteTimeUtc(this.LegacyFile), "The fix must not depend on rewriting the file.");
			Assert.AreEqual(json, await File.ReadAllTextAsync(this.LegacyFile));
		}

		[TestMethod]
		public void RestrictExistingFileToOwner_OnUnix_ShouldKeepOwnerBits()
		{
			if (OperatingSystem.IsWindows()) return;

			File.WriteAllText(this.LegacyFile, "{}");
			File.SetUnixFileMode(this.LegacyFile, UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead);

			TokenCache.RestrictExistingFileToOwner(this.LegacyFile);

			Assert.AreEqual(UnixFileMode.UserRead, File.GetUnixFileMode(this.LegacyFile));
		}

		[TestMethod]
		public void RestrictExistingFileToOwner_WhenFileIsMissing_ShouldNotThrow()
		{
			TokenCache.RestrictExistingFileToOwner(Path.Combine(this.directory, "missing.json"));
		}


		// ── Retirement after a successful SDK-cache connection ───────────────

		[TestMethod]
		public async Task TryRetire_ShouldRemoveTheProfileEntry()
		{
			await TokenCache.SaveAccessTokenAsync(this.directory, "Dev", ServiceUri, "token-dev");
			await TokenCache.SaveAccessTokenAsync(this.directory, "Uat", ServiceUri, "token-uat");

			var completed = await OrganizationServiceRepository.TryRetireLegacyTokenAsync(this.directory, "Dev");

			Assert.IsTrue(completed);
			Assert.IsNull(await TokenCache.TryGetAccessTokenAsync(this.directory, "Dev"));
			Assert.IsNotNull(await TokenCache.TryGetAccessTokenAsync(this.directory, "Uat"), "Other profiles may still be in legacy mode.");
		}

		[TestMethod]
		public async Task TryRetire_IsIdempotent()
		{
			await TokenCache.SaveAccessTokenAsync(this.directory, "Dev", ServiceUri, "token-dev");

			Assert.IsTrue(await OrganizationServiceRepository.TryRetireLegacyTokenAsync(this.directory, "Dev"));
			Assert.IsTrue(await OrganizationServiceRepository.TryRetireLegacyTokenAsync(this.directory, "Dev"));
			Assert.IsNull(await TokenCache.TryGetAccessTokenAsync(this.directory, "Dev"));
		}

		[TestMethod]
		public async Task TryRetire_WithCorruptFile_ShouldNotThrowAndLeaveTheFileAlone()
		{
			await File.WriteAllTextAsync(this.LegacyFile, "{ this is not json");

			var completed = await OrganizationServiceRepository.TryRetireLegacyTokenAsync(this.directory, "Dev");

			Assert.IsFalse(completed, "Cleanup is best effort: a broken legacy file must never break a working connection.");
			Assert.AreEqual("{ this is not json", await File.ReadAllTextAsync(this.LegacyFile));
		}

		[TestMethod]
		public async Task TryRetire_WithLockedFile_ShouldNotThrow()
		{
			await TokenCache.SaveAccessTokenAsync(this.directory, "Dev", ServiceUri, "token-dev");

			bool completed;
			using (new FileStream(this.LegacyFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				completed = await OrganizationServiceRepository.TryRetireLegacyTokenAsync(this.directory, "Dev");
			}

			if (OperatingSystem.IsWindows())
			{
				Assert.IsFalse(completed, "Another process holding the file must not make the connection fail.");
			}
		}


		// ── Warning at profile creation in LegacyPacxCache mode ──────────────

		private static (OrganizationServiceRepository repository, OutputToMemory output, Mock<ISecureTokenStorageProbe> probe) CreateRepository(bool secureStorageAvailable)
		{
			var probe = new Mock<ISecureTokenStorageProbe>(MockBehavior.Strict);
			probe.Setup(p => p.IsAvailable()).Returns(secureStorageAvailable);

			var output = new OutputToMemory();
			var repository = new OrganizationServiceRepository(output, new Mock<ISettingsRepository>().Object, new Mock<IPacxProjectRepository>().Object, probe.Object);
			return (repository, output, probe);
		}

		[TestMethod]
		public void LegacyWarning_OAuthWithoutSecureStorage_ShouldExplainTheRisksAndTheAlternative()
		{
			var (repository, output, _) = CreateRepository(secureStorageAvailable: false);

			var written = repository.WriteLegacyCacheWarningIfNeeded("AuthType=OAuth;Url=https://dev.crm4.dynamics.com", () => this.directory);

			Assert.IsTrue(written);
			var text = output.ToString();
			Assert.IsTrue(text.Contains("plain text", StringComparison.Ordinal), text);
			Assert.IsTrue(text.Contains("cannot be refreshed", StringComparison.Ordinal), text);
			Assert.IsTrue(text.Contains("ClientSecret or Certificate", StringComparison.Ordinal), text);
			Assert.IsTrue(text.Contains(this.LegacyFile, StringComparison.Ordinal), "The message must show the legacy file path: " + text);
		}

		[TestMethod]
		public void LegacyWarning_OAuthWithSecureStorage_ShouldNotBeShown()
		{
			var (repository, output, _) = CreateRepository(secureStorageAvailable: true);

			var written = repository.WriteLegacyCacheWarningIfNeeded("AuthType=OAuth;Url=https://dev.crm4.dynamics.com", DirectoryMustNotBeResolved);

			Assert.IsFalse(written);
			Assert.AreEqual(string.Empty, output.ToString().Trim());
		}

		[TestMethod]
		public void LegacyWarning_NonOAuth_ShouldNotBeShownNorProbeTheStorage()
		{
			var (repository, output, probe) = CreateRepository(secureStorageAvailable: false);

			var written = repository.WriteLegacyCacheWarningIfNeeded("AuthType=ClientSecret;Url=https://dev.crm4.dynamics.com;ClientId=id;ClientSecret=s", DirectoryMustNotBeResolved);

			Assert.IsFalse(written);
			Assert.AreEqual(string.Empty, output.ToString().Trim());
			probe.Verify(p => p.IsAvailable(), Times.Never);
		}

		// Resolving the real cache folder would create directories under the user's LocalApplicationData.
		private static string DirectoryMustNotBeResolved() =>
			throw new AssertFailedException("The token cache directory must only be resolved when the warning is shown.");
	}
}
