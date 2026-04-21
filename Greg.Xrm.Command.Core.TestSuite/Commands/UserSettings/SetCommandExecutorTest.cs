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
			repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);
			return (output, repoMock, crmMock);
		}

		// ?? Happy path — integer field, current user ???????????????????????????????

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
			var result = await executor.ExecuteAsync(
				new SetCommand { PagingLimit = 100 },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual("usersettings", capturedUpdate.LogicalName);
			Assert.AreEqual(userId, capturedUpdate.Id);
			Assert.AreEqual(100, capturedUpdate.GetAttributeValue<int>("paginglimit"));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
		}

		// ?? Happy path — boolean field, current user ???????????????????????????????

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public async Task ExecuteAsync_ShouldSucceed_BooleanField(bool value)
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
				new SetCommand { ShowWeekNumber = value },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(value, capturedUpdate.GetAttributeValue<bool>("showweeknumber"));
		}

		// ?? Happy path — picklist via enum ?????????????????????????????????????????

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_EnumPicklistField()
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
				new SetCommand { TimeFormatCodeValue = SetCommand.TimeFormat.TwentyFourHour },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(1, capturedUpdate.GetAttributeValue<int>("timeformatcode"));
		}

		// ?? Happy path — language field triggers Dataverse availability check ?????

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
			var result = await executor.ExecuteAsync(
				new SetCommand { UILanguageId = 1040 },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("uilanguageid"));
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<RetrieveAvailableLanguagesRequest>()), Times.Once);
		}

		// ?? Happy path — specific user by domain name ??????????????????????????????

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
				new SetCommand { UserDomainName = @"DOMAIN\john.doe", PagingLimit = 250 },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(userId, capturedUpdate.Id);
			Assert.AreEqual(250, capturedUpdate.GetAttributeValue<int>("paginglimit"));
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()), Times.Never);
		}

		// ?? Happy path: multiple fields in a single call ???????????????????????????

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WithMultipleSettings()
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
				new SetCommand
				{
					UILanguageId = 1040,
					HelpLanguageId = 1033,
					PagingLimit = 250,
					ShowWeekNumber = true
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("uilanguageid"));
			Assert.AreEqual(1033, capturedUpdate.GetAttributeValue<int>("helplanguageid"));
			Assert.AreEqual(250, capturedUpdate.GetAttributeValue<int>("paginglimit"));
			Assert.AreEqual(true, capturedUpdate.GetAttributeValue<bool>("showweeknumber"));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<RetrieveAvailableLanguagesRequest>()), Times.Once);
		}

		// ?? Failure: no settings provided (defensive — Validate() normally catches this) ??

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoSettingsProvided()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new SetCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new SetCommand(),
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No user setting");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Never);
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		// ?? Failure: language not available in Dataverse ???????????????????????????

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
			var result = await executor.ExecuteAsync(
				new SetCommand { UILanguageId = 1040 },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "1040");
			StringAssert.Contains(result.ErrorMessage, "not available");
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		// ?? Failure: user not found by domain name ?????????????????????????????????

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenUserNotFound()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection());

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				new SetCommand { UserDomainName = @"DOMAIN\ghost", PagingLimit = 50 },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, @"DOMAIN\ghost");
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		// ?? Failure: Dataverse fault ???????????????????????????????????????????????

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrowsFault()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var executor = new SetCommandExecutor(output, repoMock.Object);
			var result = await executor.ExecuteAsync(
				new SetCommand { PagingLimit = 50 },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}
	}
}
