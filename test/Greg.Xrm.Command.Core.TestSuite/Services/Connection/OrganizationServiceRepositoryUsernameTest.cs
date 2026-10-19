using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Issue #217: after a sign-in, the UPN must be stored in OAuth profiles that do not have one, otherwise the
	/// SDK cannot find the account in its MSAL cache and every command signs in again. These tests pin the rules,
	/// including the atomic update on fresh settings required by the review of PR #257: the in-memory snapshot is
	/// read before the sign-in and must never overwrite what another PACX process changed meanwhile.
	/// </summary>
	[TestClass]
	public class OrganizationServiceRepositoryUsernameTest
	{
		private const string ProfileName = "Dev";
		private const string OAuthProfile = "AuthType=OAuth;Url=https://dev.crm4.dynamics.com;RedirectUri=http://localhost;LoginPrompt=Auto";
		private const string ClientSecretProfile = "AuthType=ClientSecret;Url=https://dev.crm4.dynamics.com;ClientId=id;ClientSecret=secret";
		private const string SignedInUser = "user@contoso.com";

		private static byte[] Key => OrganizationServiceRepository.GetAesKey();
		private static byte[] IV => OrganizationServiceRepository.GetAesIV();


		/// <summary>
		/// Simulates connections.json: <see cref="Snapshot"/> is what the repository caches at startup,
		/// <see cref="Disk"/> is what UpdateAsync reads under the lock and what it commits.
		/// </summary>
		private sealed class Sut
		{
			public required OrganizationServiceRepository Repository { get; init; }
			public required Mock<IAtomicSettingsRepository> Settings { get; init; }
			public required OutputToMemory Output { get; init; }
			public required ConnectionSetting Snapshot { get; init; }
			public ConnectionSetting? Disk { get; set; }
			public int Commits { get; set; }

			public string? DiskConnectionString(string name)
			{
				string? connectionString = null;
				this.Disk?.TryGetConnectionString(name, Key, IV, out connectionString);
				return connectionString;
			}
		}

		private static ConnectionSetting CreateSetting(string? storedConnectionString)
		{
			var connectionStrings = new Dictionary<string, string>();
			if (storedConnectionString != null) connectionStrings[ProfileName] = storedConnectionString;

			var setting = JsonConvert.DeserializeObject<ConnectionSetting>(
				JsonConvert.SerializeObject(new { ConnectionStrings = connectionStrings, CurrentConnectionStringKey = ProfileName, IsSecured = false }))!;

			// Secure upfront, as on disk: otherwise the repository would re-save the settings on first read.
			setting.SecureSettings(Key, IV);
			return setting;
		}

		private static Sut CreateSut(string? storedConnectionString)
		{
			var snapshot = CreateSetting(storedConnectionString);
			var disk = CreateSetting(storedConnectionString);

			var settings = new Mock<IAtomicSettingsRepository>(MockBehavior.Strict);
			var project = new Mock<IPacxProjectRepository>();
			project.Setup(p => p.GetCurrentProjectAsync()).ReturnsAsync((PacxProjectDefinition?)null);
			var output = new OutputToMemory();

			var sut = new Sut
			{
				Repository = new OrganizationServiceRepository(output, settings.Object, project.Object, new Mock<ISecureTokenStorageProbe>(MockBehavior.Strict).Object),
				Settings = settings,
				Output = output,
				Snapshot = snapshot,
				Disk = disk,
			};

			settings.Setup(s => s.GetAsync<ConnectionSetting>("connections")).ReturnsAsync(snapshot);
			settings
				.Setup(s => s.UpdateAsync<ConnectionSetting>("connections", It.IsAny<Func<ConnectionSetting?, ConnectionSetting>>()))
				.Returns<string, Func<ConnectionSetting?, ConnectionSetting>>((_, update) =>
				{
					// Like the real repository: if the callback throws, nothing is committed.
					var updated = update(sut.Disk);
					sut.Disk = updated;
					sut.Commits++;
					return Task.FromResult(updated);
				});

			return sut;
		}


		// ── Basic rules ───────────────────────────────────────────────────────

		[TestMethod]
		public async Task Persist_OAuthProfileWithoutUsername_ShouldStoreTheSignedInUser()
		{
			var sut = CreateSut(OAuthProfile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(1, sut.Commits);
			Assert.AreEqual(OAuthProfile + ";Username=" + SignedInUser, sut.DiskConnectionString(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenProfileAlreadyHasTheSameUser_ShouldNotSaveOrWarn()
		{
			var stored = OAuthProfile + ";Username=USER@contoso.com";
			var sut = CreateSut(stored);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, stored, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.AreEqual(stored, sut.DiskConnectionString(ProfileName));
			Assert.AreEqual(string.Empty, sut.Output.ToString().Trim());
		}

		[TestMethod]
		public async Task Persist_WhenProfileHasADifferentUser_ShouldWarnAndNeverOverwrite()
		{
			var stored = OAuthProfile + ";Username=configured@contoso.com";
			var sut = CreateSut(stored);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, stored, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.AreEqual(stored, sut.DiskConnectionString(ProfileName));
			var text = sut.Output.ToString();
			Assert.IsTrue(text.Contains("configured@contoso.com", StringComparison.Ordinal), text);
			Assert.IsTrue(text.Contains(SignedInUser, StringComparison.Ordinal), text);
		}

		[TestMethod]
		public async Task Persist_ForNonOAuthProfile_ShouldDoNothing()
		{
			var sut = CreateSut(ClientSecretProfile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, ClientSecretProfile, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.AreEqual(ClientSecretProfile, sut.DiskConnectionString(ProfileName));
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("  ")]
		public async Task Persist_WithoutSignedInUser_ShouldDoNothing(string? signedInUser)
		{
			var sut = CreateSut(OAuthProfile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, signedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.AreEqual(OAuthProfile, sut.DiskConnectionString(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenProfileIsNotStoredYet_ShouldDoNothing()
		{
			// This is the auth create flow: SetConnectionAsync adds the user name itself before saving.
			var sut = CreateSut(storedConnectionString: null);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.IsNull(sut.DiskConnectionString(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenSettingsFileDoesNotExist_ShouldNotCreateIt()
		{
			var sut = CreateSut(OAuthProfile);
			sut.Disk = null;

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.IsNull(sut.Disk);
		}


		// ── Concurrency (review of PR #257) ───────────────────────────────────

		[TestMethod]
		public async Task Persist_ShouldKeepChangesMadeByAnotherProcessDuringTheSignIn()
		{
			var sut = CreateSut(OAuthProfile);
			await sut.Repository.GetAllConnectionDefinitionsAsync(); // snapshot cached at startup, before the sign-in

			// Meanwhile another PACX process creates a profile and makes it the default.
			sut.Disk!.UpsertConnectionString("Other", ClientSecretProfile, Key, IV);
			sut.Disk.CurrentConnectionStringKey = "Other";

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(1, sut.Commits);
			Assert.AreEqual(OAuthProfile + ";Username=" + SignedInUser, sut.DiskConnectionString(ProfileName));
			Assert.AreEqual(ClientSecretProfile, sut.DiskConnectionString("Other"), "A profile created concurrently must not be lost.");
			Assert.AreEqual("Other", sut.Disk.CurrentConnectionStringKey, "A default changed concurrently must not be reverted.");
			Assert.IsFalse(sut.Snapshot.Exists("Other"), "Sanity check: the stale snapshot did not contain the new profile.");
		}

		[TestMethod]
		public async Task Persist_ShouldRefreshTheInMemoryCopyWithTheFreshSettings()
		{
			var sut = CreateSut(OAuthProfile);
			await sut.Repository.GetAllConnectionDefinitionsAsync();
			sut.Disk!.UpsertConnectionString("Other", ClientSecretProfile, Key, IV);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			var inMemory = await sut.Repository.GetAllConnectionDefinitionsAsync();
			Assert.IsTrue(inMemory.Exists("Other"), "Later saves in this process must not start again from the stale snapshot.");
		}

		[TestMethod]
		public async Task Persist_WhenProfileWasDeletedByAnotherProcess_ShouldNotRecreateIt()
		{
			var sut = CreateSut(OAuthProfile);
			await sut.Repository.GetAllConnectionDefinitionsAsync();
			sut.Disk!.Remove(ProfileName);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.IsFalse(sut.Disk.Exists(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenProfileWasChangedByAnotherProcess_ShouldNotOverwriteIt()
		{
			var changedMeanwhile = OAuthProfile.Replace("dev.crm4", "dev2.crm4", StringComparison.Ordinal);
			var sut = CreateSut(OAuthProfile);
			await sut.Repository.GetAllConnectionDefinitionsAsync();
			sut.Disk!.UpsertConnectionString(ProfileName, changedMeanwhile, Key, IV);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			Assert.AreEqual(changedMeanwhile, sut.DiskConnectionString(ProfileName));
		}

		[TestMethod]
		[DataRow(typeof(IOException))]
		[DataRow(typeof(UnauthorizedAccessException))]
		public async Task Persist_WhenSavingFails_ShouldWarnAndNotFailTheAuthenticatedCommand(Type failure)
		{
			// Review of PR #257: the SDK client is already authenticated; the Username is only an optimization.
			var sut = CreateSut(OAuthProfile);
			sut.Settings
				.Setup(s => s.UpdateAsync<ConnectionSetting>("connections", It.IsAny<Func<ConnectionSetting?, ConnectionSetting>>()))
				.ThrowsAsync((Exception)Activator.CreateInstance(failure, "settings file is locked")!);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(OAuthProfile, sut.DiskConnectionString(ProfileName));
			var text = sut.Output.ToString();
			Assert.IsTrue(text.Contains("could not save the signed-in user", StringComparison.Ordinal), text);
			Assert.IsTrue(text.Contains("settings file is locked", StringComparison.Ordinal), text);
		}

		[TestMethod]
		public async Task Persist_WhenStoredProfileIsMalformed_ShouldWarnAndNotFailTheAuthenticatedCommand()
		{
			// Review of PR #257: a corrupted encrypted value makes Convert.FromBase64String throw FormatException.
			var sut = CreateSut(OAuthProfile);
			sut.Disk = JsonConvert.DeserializeObject<ConnectionSetting>(
				JsonConvert.SerializeObject(new { ConnectionStrings = new Dictionary<string, string> { [ProfileName] = "not-valid-base64!!" }, IsSecured = true }))!;

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			Assert.AreEqual(0, sut.Commits);
			var text = sut.Output.ToString();
			Assert.IsTrue(text.Contains("could not save the signed-in user", StringComparison.Ordinal), text);
		}

		[TestMethod]
		public async Task Persist_WithoutAtomicSettingsRepository_ShouldNotWriteAnything()
		{
			// Without a cross-process atomic update the only safe choice is not to write: a missing Username
			// costs one more sign-in, a lost concurrent change would lose user data.
			var settings = new Mock<ISettingsRepository>(MockBehavior.Strict);
			settings.Setup(s => s.GetAsync<ConnectionSetting>("connections")).ReturnsAsync(CreateSetting(OAuthProfile));
			var project = new Mock<IPacxProjectRepository>();
			var repository = new OrganizationServiceRepository(new OutputToMemory(), settings.Object, project.Object, new Mock<ISecureTokenStorageProbe>().Object);

			await repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
		}
	}
}
