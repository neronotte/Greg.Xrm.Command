using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	[TestClass]
	public class ChangeLanguageCommandExecutorTest
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

			repoMock
				.Setup(r => r.GetCurrentConnectionAsync())
				.ReturnsAsync(crmMock.Object);

			return (output, repoMock, crmMock);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_AndUpdateAllFields_WhenFieldIsOmitted()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;
			var userId = Guid.NewGuid();

			var availableLanguagesResponse = new RetrieveAvailableLanguagesResponse();
			availableLanguagesResponse.Results["LocaleIds"] = new[] { 1033, 1040 };

			var whoAmIResponse = new WhoAmIResponse();
			whoAmIResponse.Results["UserId"] = userId;

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest request) =>
				{
					if (request is RetrieveAvailableLanguagesRequest) return availableLanguagesResponse;
					if (request is WhoAmIRequest) return whoAmIResponse;
					throw new InvalidOperationException($"Unexpected request {request.GetType().Name}");
				});

			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new ChangeLanguageCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new ChangeLanguageCommand { Lcid = 1040 },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual("usersettings", capturedUpdate.LogicalName);
			Assert.AreEqual(userId, capturedUpdate.Id);
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("uilanguageid"));
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("helplanguageid"));
			Assert.AreEqual(1040, capturedUpdate.GetAttributeValue<int>("localeid"));

			repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Once);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<RetrieveAvailableLanguagesRequest>()), Times.Once);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()), Times.Once);
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUpdateOnlySelectedField()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			Entity? capturedUpdate = null;

			var availableLanguagesResponse = new RetrieveAvailableLanguagesResponse();
			availableLanguagesResponse.Results["LocaleIds"] = new[] { 1033 };

			var whoAmIResponse = new WhoAmIResponse();
			whoAmIResponse.Results["UserId"] = Guid.NewGuid();

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest request) =>
				{
					if (request is RetrieveAvailableLanguagesRequest) return availableLanguagesResponse;
					if (request is WhoAmIRequest) return whoAmIResponse;
					throw new InvalidOperationException($"Unexpected request {request.GetType().Name}");
				});

			crmMock
				.Setup(c => c.UpdateAsync(It.IsAny<Entity>()))
				.Callback<Entity>(e => capturedUpdate = e)
				.Returns(Task.CompletedTask);

			var executor = new ChangeLanguageCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new ChangeLanguageCommand { Lcid = 1033, Field = LanguageField.LocaleId },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(capturedUpdate);
			Assert.AreEqual(1, capturedUpdate.Attributes.Count);
			Assert.AreEqual(1033, capturedUpdate.GetAttributeValue<int>("localeid"));
			Assert.IsFalse(capturedUpdate.Contains("uilanguageid"));
			Assert.IsFalse(capturedUpdate.Contains("helplanguageid"));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenLcidIsInvalid()
		{
			var (output, repoMock, crmMock) = CreateMocks();
			var executor = new ChangeLanguageCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new ChangeLanguageCommand { Lcid = -1 },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not valid");
			repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Never);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()), Times.Never);
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenLanguageIsNotAvailableInDataverse()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			var availableLanguagesResponse = new RetrieveAvailableLanguagesResponse();
			availableLanguagesResponse.Results["LocaleIds"] = new[] { 1033 };

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync((OrganizationRequest request) =>
				{
					if (request is RetrieveAvailableLanguagesRequest) return availableLanguagesResponse;
					throw new InvalidOperationException($"Unexpected request {request.GetType().Name}");
				});

			var executor = new ChangeLanguageCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new ChangeLanguageCommand { Lcid = 1040 },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not available");
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
			crmMock.Verify(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrowsFault()
		{
			var (output, repoMock, crmMock) = CreateMocks();

			crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					"Simulated Dataverse fault"));

			var executor = new ChangeLanguageCommandExecutor(output, repoMock.Object);

			var result = await executor.ExecuteAsync(
				new ChangeLanguageCommand { Lcid = 1033 },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
			crmMock.Verify(c => c.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}
	}
}
