using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class SetCommandExecutorTest
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
			repoMock.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(crmMock.Object);
			return (output, repoMock, crmMock);
		}

		private static SetCommand SinglePair(string key, string value, string? user = null)
			=> new() { Keys = [key], Values = [value], UserDomainName = user };

		private static SetCommand MultiPair(params (string Key, string Value)[] pairs)
			=> new() { Keys = pairs.Select(p => p.Key).ToList(), Values = pairs.Select(p => p.Value).ToList() };

		// ── Happy path — single integer field, current user ───────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_IntegerField_CurrentUser()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest r) =>
				{
					if (r is WhoAmIRequest) return whoAmI;
					throw new InvalidOperationException($"Unexpected request {r.GetType().Name}");
				});
			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(SinglePair("paginglimit", "100"), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual("usersettings", capturedUpdate.LogicalName);
			Assert.AreEqual(userId, capturedUpdate.Id);
			Assert.AreEqual(100, capturedUpdate.GetAttributeValue<int>("paginglimit"));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
		}

		// ── Happy path — single boolean field ─────────────────────────────────────

		[TestMethod]
		[DataRow("true", true)]
		[DataRow("false", false)]
		[DataRow("1", true)]
		[DataRow("0", false)]
		[DataRow("yes", true)]
		[DataRow("no", false)]
		public async Task ExecuteAsync_ShouldSucceed_BooleanField(string rawValue, bool expected)
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>())).ReturnsAsync(whoAmI);
			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(SinglePair("showweeknumber", rawValue), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(expected, capturedUpdate.GetAttributeValue<bool>("showweeknumber"));
		}

		// ── Happy path — single language field ────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_LanguageField_ValidatesDataverse()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var langResponse = new RetrieveAvailableLanguagesResponse();
			langResponse.Results["LocaleIds"] = new[] { 1033, 1040 };

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest r) =>
				{
					if (r is RetrieveAvailableLanguagesRequest) return langResponse;
					if (r is WhoAmIRequest) return whoAmI;
					throw new InvalidOperationException($"Unexpected request {r.GetType().Name}");
				});
			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(SinglePair("uilanguageid", "1040"), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("uilanguageid"));
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<RetrieveAvailableLanguagesRequest>()), Times.Once);
		}

		// ── Happy path — explicit user ────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WithExplicitUser()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var userEntity = new Entity("systemuser") { Id = userId };
			userEntity["fullname"] = "John Doe";
			userEntity["domainname"] = @"DOMAIN\john.doe";

			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { userEntity }));

			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				SinglePair("paginglimit", "250", @"DOMAIN\john.doe"),
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(userId, capturedUpdate.Id);
			Assert.AreEqual(250, capturedUpdate.GetAttributeValue<int>("paginglimit"));
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()), Times.Never);
		}

		// ── Happy path — multiple key/value pairs ─────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_MultiplePairs_AllInOneUpdate()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var langResponse = new RetrieveAvailableLanguagesResponse();
			langResponse.Results["LocaleIds"] = new[] { 1033, 1040 };

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest r) =>
				{
					if (r is RetrieveAvailableLanguagesRequest) return langResponse;
					if (r is WhoAmIRequest) return whoAmI;
					throw new InvalidOperationException($"Unexpected request {r.GetType().Name}");
				});
			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				MultiPair(("uilanguageid", "1040"), ("helplanguageid", "1040"), ("localeid", "1040")),
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(3, capturedUpdate.Attributes.Count);
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("uilanguageid"));
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("helplanguageid"));
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("localeid"));
			// Language check happens once even though three language fields are set
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<RetrieveAvailableLanguagesRequest>()), Times.Once);
			// Only a single Update call regardless of how many fields
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_MixedFields_SingleUpdate()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = userId;

			crmMock.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>())).ReturnsAsync(whoAmI);
			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				MultiPair(("paginglimit", "250"), ("showweeknumber", "true"), ("timeformatcode", "1")),
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(3, capturedUpdate.Attributes.Count);
			Assert.AreEqual(250, capturedUpdate.GetAttributeValue<int>("paginglimit"));
			Assert.AreEqual(true, capturedUpdate.GetAttributeValue<bool>("showweeknumber"));
			Assert.AreEqual(1, capturedUpdate.GetAttributeValue<int>("timeformatcode"));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
		}

		// ── Failure: key/value count mismatch ─────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenKeyValueCountMismatch()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new SetCommand { Keys = ["paginglimit", "showweeknumber"], Values = ["100"] },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "2");
			StringAssert.Contains(result.ErrorMessage, "1");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Failure: unknown key ───────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenKeyIsUnknown()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(SinglePair("notafield", "abc"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "notafield");
			StringAssert.Contains(result.ErrorMessage, "supported");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenSecondKeyIsUnknown()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				MultiPair(("paginglimit", "100"), ("notafield", "abc")),
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "notafield");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Failure: invalid picklist value ───────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenPicklistValueIsInvalid()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(SinglePair("timeformatcode", "99"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "99");
			StringAssert.Contains(result.ErrorMessage, "Allowed values");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Failure: non-integer value for integer field ───────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenIntegerValueIsNotNumeric()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(SinglePair("paginglimit", "abc"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "abc");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Failure: invalid boolean ───────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenBooleanValueIsInvalid()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(SinglePair("showweeknumber", "maybe"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "maybe");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Failure: invalid LCID ─────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenLcidIsInvalid()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(SinglePair("uilanguageid", "-1"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "-1");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Failure: language not available in Dataverse ───────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenLanguageNotAvailableInDataverse()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			var langResponse = new RetrieveAvailableLanguagesResponse();
			langResponse.Results["LocaleIds"] = new[] { 1033 };

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest r) =>
				{
					if (r is RetrieveAvailableLanguagesRequest) return langResponse;
					throw new InvalidOperationException($"Unexpected: {r.GetType().Name}");
				});

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(SinglePair("uilanguageid", "1040"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "1040");
			StringAssert.Contains(result.ErrorMessage, "not available");
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		// ── Failure: user not found by domain name ─────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenUserNotFound()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection());

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				SinglePair("paginglimit", "50", @"DOMAIN\ghost"),
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, @"DOMAIN\ghost");
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		// ── Failure: Dataverse fault ───────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrowsFault()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(SinglePair("paginglimit", "50"), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}
	}
}
