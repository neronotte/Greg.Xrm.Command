using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Issue #217: after a sign-in, the UPN must be stored in OAuth profiles that do not have one, otherwise the
	/// SDK cannot find the account in its MSAL cache and every command signs in again. These tests pin the rules.
	/// </summary>
	[TestClass]
	public class OrganizationServiceRepositoryUsernameTest
	{
		private const string ProfileName = "Dev";
		private const string OAuthProfile = "AuthType=OAuth;Url=https://dev.crm4.dynamics.com;RedirectUri=http://localhost;LoginPrompt=Auto";
		private const string ClientSecretProfile = "AuthType=ClientSecret;Url=https://dev.crm4.dynamics.com;ClientId=id;ClientSecret=secret";
		private const string SignedInUser = "user@contoso.com";


		private sealed class Sut
		{
			public required OrganizationServiceRepository Repository { get; init; }
			public required Mock<ISettingsRepository> Settings { get; init; }
			public required OutputToMemory Output { get; init; }
			public required ConnectionSetting Setting { get; init; }

			public string? StoredConnectionString(string name)
			{
				this.Setting.TryGetConnectionString(name, OrganizationServiceRepository.GetAesKey(), OrganizationServiceRepository.GetAesIV(), out var connectionString);
				return connectionString;
			}
		}

		private static Sut CreateSut(string? storedConnectionString)
		{
			var setting = JsonConvert.DeserializeObject<ConnectionSetting>("{ \"IsSecured\": false }")!;
			if (storedConnectionString != null)
			{
				setting = JsonConvert.DeserializeObject<ConnectionSetting>(
					JsonConvert.SerializeObject(new { ConnectionStrings = new Dictionary<string, string> { [ProfileName] = storedConnectionString }, IsSecured = false }))!;
			}

			// Secure upfront, as on disk: otherwise the repository would re-save the settings on first read,
			// which would hide whether the username logic saved anything.
			setting.SecureSettings(OrganizationServiceRepository.GetAesKey(), OrganizationServiceRepository.GetAesIV());

			var settings = new Mock<ISettingsRepository>();
			settings.Setup(s => s.GetAsync<ConnectionSetting>("connections")).ReturnsAsync(setting);
			settings.Setup(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>())).Returns(Task.CompletedTask);

			var project = new Mock<IPacxProjectRepository>();
			project.Setup(p => p.GetCurrentProjectAsync()).ReturnsAsync((PacxProjectDefinition?)null);

			var probe = new Mock<ISecureTokenStorageProbe>(MockBehavior.Strict);
			var output = new OutputToMemory();

			return new Sut
			{
				Repository = new OrganizationServiceRepository(output, settings.Object, project.Object, probe.Object),
				Settings = settings,
				Output = output,
				Setting = setting,
			};
		}


		[TestMethod]
		public async Task Persist_OAuthProfileWithoutUsername_ShouldStoreTheSignedInUser()
		{
			var sut = CreateSut(OAuthProfile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			sut.Settings.Verify(s => s.SetAsync("connections", It.IsAny<ConnectionSetting>()), Times.Once);
			Assert.AreEqual(OAuthProfile + ";Username=" + SignedInUser, sut.StoredConnectionString(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenProfileAlreadyHasTheSameUser_ShouldNotSaveOrWarn()
		{
			var stored = OAuthProfile + ";Username=USER@contoso.com";
			var sut = CreateSut(stored);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, stored, SignedInUser);

			sut.Settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
			Assert.AreEqual(stored, sut.StoredConnectionString(ProfileName));
			Assert.AreEqual(string.Empty, sut.Output.ToString().Trim());
		}

		[TestMethod]
		public async Task Persist_WhenProfileHasADifferentUser_ShouldWarnAndNeverOverwrite()
		{
			var stored = OAuthProfile + ";Username=configured@contoso.com";
			var sut = CreateSut(stored);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, stored, SignedInUser);

			sut.Settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
			Assert.AreEqual(stored, sut.StoredConnectionString(ProfileName));
			var text = sut.Output.ToString();
			Assert.IsTrue(text.Contains("configured@contoso.com", StringComparison.Ordinal), text);
			Assert.IsTrue(text.Contains(SignedInUser, StringComparison.Ordinal), text);
		}

		[TestMethod]
		public async Task Persist_ForNonOAuthProfile_ShouldDoNothing()
		{
			var sut = CreateSut(ClientSecretProfile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, ClientSecretProfile, SignedInUser);

			sut.Settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
			Assert.AreEqual(ClientSecretProfile, sut.StoredConnectionString(ProfileName));
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("  ")]
		public async Task Persist_WithoutSignedInUser_ShouldDoNothing(string? signedInUser)
		{
			var sut = CreateSut(OAuthProfile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, signedInUser);

			sut.Settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
			Assert.AreEqual(OAuthProfile, sut.StoredConnectionString(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenStoredProfileChangedMeanwhile_ShouldNotOverwriteIt()
		{
			var changedMeanwhile = OAuthProfile.Replace("dev.crm4", "dev2.crm4", StringComparison.Ordinal);
			var sut = CreateSut(changedMeanwhile);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			sut.Settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
			Assert.AreEqual(changedMeanwhile, sut.StoredConnectionString(ProfileName));
		}

		[TestMethod]
		public async Task Persist_WhenProfileIsNotStoredYet_ShouldDoNothing()
		{
			// This is the auth create flow: SetConnectionAsync adds the user name itself before saving.
			var sut = CreateSut(storedConnectionString: null);

			await sut.Repository.PersistAuthenticatedUsernameAsync(ProfileName, OAuthProfile, SignedInUser);

			sut.Settings.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()), Times.Never);
			Assert.IsNull(sut.StoredConnectionString(ProfileName));
		}
	}
}
