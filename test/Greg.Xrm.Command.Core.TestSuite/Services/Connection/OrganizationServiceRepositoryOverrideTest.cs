using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
	[TestClass]
	public class OrganizationServiceRepositoryOverrideTest
	{
		/// <summary>
		/// Builds an OrganizationServiceRepository with a controlled ConnectionSetting.
		/// Connection strings are stored as plaintext with IsSecured=false so that the
		/// repository's internal SecureSettings call encrypts them with the real embedded AES key.
		/// </summary>
		private static OrganizationServiceRepository CreateSut(
			string currentConnectionStringKey,
			Dictionary<string, string> plaintextConnections)
		{
			var connectionStringsJson = string.Join(",\n",
				plaintextConnections.Select(kv => $"\"{kv.Key}\": \"{kv.Value}\""));

			var json = $$"""
				{
					"ConnectionStrings": { {{connectionStringsJson}} },
					"CurrentConnectionStringKey": "{{currentConnectionStringKey}}",
					"IsSecured": false
				}
				""";

			var setting = JsonConvert.DeserializeObject<ConnectionSetting>(json)!;

			var settingsMock = new Mock<ISettingsRepository>();
			settingsMock
				.Setup(s => s.GetAsync<ConnectionSetting>("connections"))
				.ReturnsAsync(setting);
			settingsMock
				.Setup(s => s.SetAsync(It.IsAny<string>(), It.IsAny<ConnectionSetting>()))
				.Returns(Task.CompletedTask);

			var projectMock = new Mock<IPacxProjectRepository>();
			projectMock
				.Setup(p => p.GetCurrentProjectAsync())
				.ReturnsAsync((PacxProjectDefinition?)null);

			var output = new OutputToMemory();
			return new OrganizationServiceRepository(output, settingsMock.Object, projectMock.Object);
		}

		// ── Name-based override ───────────────────────────────────────────────

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideByName_ShouldReturnOverrideName()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["Dev"]  = "Url=https://dev.crm.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("Dev");

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("Dev", name);
		}

		// ── URL-based override ────────────────────────────────────────────────

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideByUrl_ShouldReturnMatchingProfileName()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["Dev"]  = "Url=https://dev.crm.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("https://dev.crm.dynamics.com");

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("Dev", name);
		}

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideByUrl_TrailingSlashNormalized()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["Dev"]  = "Url=https://dev.crm.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("https://dev.crm.dynamics.com/");

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("Dev", name);
		}

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideByUrl_CaseInsensitive()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["Dev"]  = "Url=https://dev.crm.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("HTTPS://DEV.CRM.DYNAMICS.COM");

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("Dev", name);
		}

		// ── Not-found → exception ─────────────────────────────────────────────

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideNotFound_ShouldThrowCommandException()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("NonExistent");

			await Assert.ThrowsExactlyAsync<CommandException>(
			async () => await repo.GetCurrentConnectionNameAsync());
		}

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideNotFound_ExceptionCodeIsConnectionNotSet()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("NonExistent");

			var ex = await Assert.ThrowsExactlyAsync<CommandException>(
			async () => await repo.GetCurrentConnectionNameAsync());
			Assert.AreEqual(CommandException.ConnectionNotSet, ex.ErrorCode);
		}

		// ── No override → existing behavior ───────────────────────────────────

		[TestMethod]
		public async Task GetCurrentConnectionName_WithNoOverride_ShouldUseGlobalDefault()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["Dev"]  = "Url=https://dev.crm.dynamics.com;AuthType=OAuth",
			});

			// No SetEnvironmentOverride call — should fall back to CurrentConnectionStringKey

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("Prod", name);
		}

		// ── Case-insensitive name match ────────────────────────────────────────

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideByName_CaseInsensitive()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["RG01"] = "Url=https://rg01.crm4.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("rg01"); // lowercase, stored key is "RG01"

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("RG01", name); // returns the stored casing
		}

		[TestMethod]
		public async Task GetCurrentConnectionName_WhenOverrideByName_MixedCase()
		{
			var repo = CreateSut("Prod", new Dictionary<string, string>
			{
				["Prod"] = "Url=https://prod.crm.dynamics.com;AuthType=OAuth",
				["AcnBpo-DEV"] = "Url=https://acnbpo-dev.crm4.dynamics.com;AuthType=OAuth",
			});

			repo.SetEnvironmentOverride("ACNBPO-DEV"); // uppercase, stored key is "AcnBpo-DEV"

			var name = await repo.GetCurrentConnectionNameAsync();

			Assert.AreEqual("AcnBpo-DEV", name);
		}
	}
}
