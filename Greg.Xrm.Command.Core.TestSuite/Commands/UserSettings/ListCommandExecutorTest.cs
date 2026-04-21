using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class ListCommandExecutorTest
	{
		private static (
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IOrganizationServiceAsync2> crmMock)
		CreateMocks()
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var repoMock = new Mock<IOrganizationServiceRepository>();
			repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);
			return (output, repoMock, crmMock);
		}

		/// <summary>Builds a minimal usersettings entity with a handful of well-known fields.</summary>
		private static Entity BuildSettingsEntity(Guid userId)
		{
			var e = new Entity("usersettings") { Id = userId };
			e["uilanguageid"]   = 1040;
			e["helplanguageid"] = 1040;
			e["localeid"]       = 1040;
			e["paginglimit"]    = 250;
			e["showweeknumber"] = true;
			e["timeformatcode"] = 1;
			return e;
		}

		/// <summary>
		/// Sets up <see cref="IOrganizationServiceAsync2.RetrieveMultipleAsync"/> so that
		/// systemuser queries return <paramref name="userEntities"/> and usersettings queries
		/// return <paramref name="settingsEntities"/>.
		/// </summary>
		private static void SetupRetrieveMultiple(
			Mock<IOrganizationServiceAsync2> crmMock,
			EntityCollection userEntities,
			EntityCollection settingsEntities)
		{
			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync((QueryBase q) =>
				{
					if (q is QueryExpression qe && qe.EntityName == "systemuser")
						return userEntities;
					if (q is QueryExpression qe2 && qe2.EntityName == "usersettings")
						return settingsEntities;
					return new EntityCollection();
				});
		}

		// ── Happy path — current user (WhoAmI) ────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_CurrentUser()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var userId = Guid.NewGuid();
			var settings = BuildSettingsEntity(userId);

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()))
				.ReturnsAsync(whoAmI);

			SetupRetrieveMultiple(crmMock, new EntityCollection(), new EntityCollection([settings]));

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(userId, result["SystemUserId"]);
			// Spot-check a few formatted values in the result dictionary
			Assert.AreEqual("1040", result["uilanguageid"]);
			Assert.AreEqual("250", result["paginglimit"]);
			Assert.AreEqual("true", result["showweeknumber"]);
			// WhoAmI must be called; no systemuser lookup expected
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()), Times.Once);
		}

		// ── Happy path — explicit user by domain name ─────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WithExplicitUser()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var userId = Guid.NewGuid();

			var userEntity = new Entity("systemuser") { Id = userId };
			userEntity["fullname"]   = "John Doe";
			userEntity["domainname"] = @"DOMAIN\john.doe";

			SetupRetrieveMultiple(
				crmMock,
				new EntityCollection([userEntity]),
				new EntityCollection([BuildSettingsEntity(userId)]));

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				new ListCommand { UserDomainName = @"DOMAIN\john.doe" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(userId, result["SystemUserId"]);
			// WhoAmI must NOT have been called
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()), Times.Never);
		}

		// ── Happy path — picklist value is formatted with its label ───────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFormatPicklistWithLabel()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var userId = Guid.NewGuid();

			// timeformatcode = 1 → "1 (TwentyFourHour)" via the enum on SetCommand.TimeFormat
			var settings = new Entity("usersettings") { Id = userId };
			settings["timeformatcode"] = 1;

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>())).ReturnsAsync(whoAmI);
			SetupRetrieveMultiple(crmMock, new EntityCollection(), new EntityCollection([settings]));

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(result["timeformatcode"]?.ToString(), "TwentyFourHour");
		}

		// ── Happy path — boolean field rendered as lowercase string ───────────

		[TestMethod]
		[DataRow(true,  "true")]
		[DataRow(false, "false")]
		public async Task ExecuteAsync_ShouldFormatBooleanAsLowercase(bool rawValue, string expected)
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var userId = Guid.NewGuid();

			var settings = new Entity("usersettings") { Id = userId };
			settings["showweeknumber"] = rawValue;

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>())).ReturnsAsync(whoAmI);
			SetupRetrieveMultiple(crmMock, new EntityCollection(), new EntityCollection([settings]));

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(expected, result["showweeknumber"]?.ToString());
		}

		// ── Failure: user not found by domain name ────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenUserNotFound()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			// systemuser query returns empty; usersettings query should never be reached
			SetupRetrieveMultiple(crmMock, new EntityCollection(), new EntityCollection());

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				new ListCommand { UserDomainName = @"DOMAIN\ghost" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, @"DOMAIN\ghost");
		}

		// ── Failure: no usersettings record found ─────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoSettingsRecord()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var userId = Guid.NewGuid();

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>())).ReturnsAsync(whoAmI);
			// usersettings query returns empty
			SetupRetrieveMultiple(crmMock, new EntityCollection(), new EntityCollection());

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No usersettings record found");
		}

		// ── Failure: Dataverse fault ──────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrowsFault()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var executor = new ListCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
		}
	}
}
