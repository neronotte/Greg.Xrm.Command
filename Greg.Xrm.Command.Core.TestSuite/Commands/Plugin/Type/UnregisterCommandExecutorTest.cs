using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Plugin.Type
{
	[TestClass]
	public class UnregisterCommandExecutorTest
	{
		private static readonly Guid TypeId = Guid.NewGuid();
		private static readonly string TypeName = "Contoso.Plugins.AccountPlugin";

		private static (
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IOrganizationServiceAsync2> crmMock,
			Mock<IPluginTypeRepository> pluginTypeRepoMock,
			Mock<ISdkMessageProcessingStepRepository> stepRepoMock,
			Mock<ISdkMessageProcessingStepImageRepository> imageRepoMock)
		CreateMocks(PluginType? pluginTypeToReturn = null, SdkMessageProcessingStep[]? stepsToReturn = null)
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var repoMock = new Mock<IOrganizationServiceRepository>();

			repoMock
				.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(crmMock.Object);

			crmMock
				.Setup(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var pluginType = pluginTypeToReturn ?? BuildPluginType(TypeId, TypeName);

			var pluginTypeRepoMock = new Mock<IPluginTypeRepository>();
			pluginTypeRepoMock
				.Setup(r => r.GetByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(pluginType);
			pluginTypeRepoMock
				.Setup(r => r.FuzzySearchAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([pluginType]);

			var steps = stepsToReturn ?? [];

			var stepRepoMock = new Mock<ISdkMessageProcessingStepRepository>();
			stepRepoMock
				.Setup(r => r.GetByPluginTypeIdAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
				.ReturnsAsync(steps);

			var imageRepoMock = new Mock<ISdkMessageProcessingStepImageRepository>();
			imageRepoMock
				.Setup(r => r.GetByStepIdAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>()))
				.ReturnsAsync([]);

			return (output, repoMock, crmMock, pluginTypeRepoMock, stepRepoMock, imageRepoMock);
		}

		private static UnregisterCommandExecutor CreateExecutor(
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> repoMock,
			Mock<IPluginTypeRepository> pluginTypeRepoMock,
			Mock<ISdkMessageProcessingStepRepository> stepRepoMock,
			Mock<ISdkMessageProcessingStepImageRepository> imageRepoMock)
		{
			return new UnregisterCommandExecutor(
				output,
				repoMock.Object,
				pluginTypeRepoMock.Object,
				stepRepoMock.Object,
				imageRepoMock.Object);
		}

		private static PluginType BuildPluginType(Guid id, string name)
		{
			var pt = new PluginType { name = name };
			((Greg.Xrm.Command.Model.IEntityWrapperInternal)pt).SetId(id);
			return pt;
		}

		private static SdkMessageProcessingStep BuildStep(Guid id, string name)
		{
			var step = new SdkMessageProcessingStep { name = name };
			((Greg.Xrm.Command.Model.IEntityWrapperInternal)step).SetId(id);
			return step;
		}


		// ── Happy path by name — no steps ──────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenTypeFoundByNameAndHasNoSteps()
		{
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks();

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = TypeName },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			ptRepo.Verify(r => r.FuzzySearchAsync(It.IsAny<IOrganizationServiceAsync2>(), TypeName, It.IsAny<CancellationToken>()), Times.Once);
			stepRepo.Verify(r => r.GetByPluginTypeIdAsync(It.IsAny<IOrganizationServiceAsync2>(), TypeId, false, It.IsAny<CancellationToken>()), Times.Once);
			crmMock.Verify(c => c.DeleteAsync("plugintype", TypeId, It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── Happy path by ID — no steps ────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenTypeFoundByIdAndHasNoSteps()
		{
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks();

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { TypeId = TypeId },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			ptRepo.Verify(r => r.GetByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), TypeId, It.IsAny<CancellationToken>()), Times.Once);
			crmMock.Verify(c => c.DeleteAsync("plugintype", TypeId, It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── Fail — plugin type not found by fuzzy search ───────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTypeNotFoundByName()
		{
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks();

			ptRepo
				.Setup(r => r.FuzzySearchAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = "nonexistent" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			crmMock.Verify(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Fail — ambiguous plugin type ───────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNameIsAmbiguous()
		{
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks();

			ptRepo
				.Setup(r => r.FuzzySearchAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([BuildPluginType(Guid.NewGuid(), "Plugins.AccountPlugin"), BuildPluginType(Guid.NewGuid(), "Plugins2.AccountPlugin")]);

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = "AccountPlugin" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			crmMock.Verify(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Fail — plugin type not found by ID ─────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTypeNotFoundById()
		{
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks();

			ptRepo
				.Setup(r => r.GetByIdAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((PluginType?)null);

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { TypeId = Guid.NewGuid() },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			crmMock.Verify(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Fail — type has steps, --force not set ─────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenTypeHasStepsAndForceIsNotSet()
		{
			var step = BuildStep(Guid.NewGuid(), "Contoso.Plugins.AccountPlugin: Create of account (PreOperation)");
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks(stepsToReturn: [step]);

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = TypeName, Force = false },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
			crmMock.Verify(c => c.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		// ── Happy path with --force — steps and images deleted ─────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenTypeHasStepsAndForceIsSet()
		{
			var stepId = Guid.NewGuid();
			var imageId = Guid.NewGuid();

			var step = BuildStep(stepId, "Contoso.Plugins.AccountPlugin: Create of account (PreOperation)");
			var image = new SdkMessageProcessingStepImage { name = "PreImage" };
			((Greg.Xrm.Command.Model.IEntityWrapperInternal)image).SetId(imageId);

			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks(stepsToReturn: [step]);

			imageRepo
				.Setup(r => r.GetByStepIdAsync(It.IsAny<IOrganizationServiceAsync2>(), stepId))
				.ReturnsAsync([image]);

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = TypeName, Force = true },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

			// Image deleted first
			crmMock.Verify(c => c.DeleteAsync("sdkmessageprocessingstepimage", imageId, It.IsAny<CancellationToken>()), Times.Once);
			// Then step
			crmMock.Verify(c => c.DeleteAsync("sdkmessageprocessingstep", stepId, It.IsAny<CancellationToken>()), Times.Once);
			// Then plugin type
			crmMock.Verify(c => c.DeleteAsync("plugintype", TypeId, It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── Happy path with --force — secure config also deleted ───────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldDeleteSecureConfig_WhenStepHasOne()
		{
			var stepId = Guid.NewGuid();
			var secureConfigId = Guid.NewGuid();

			var step = BuildStep(stepId, "Contoso.Plugins.AccountPlugin: Create of account (PreOperation)");
			step.sdkmessageprocessingstepsecureconfigid = new EntityReference("sdkmessageprocessingstepsecureconfig", secureConfigId);

			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks(stepsToReturn: [step]);

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = TypeName, Force = true },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			crmMock.Verify(c => c.DeleteAsync("sdkmessageprocessingstepsecureconfig", secureConfigId, It.IsAny<CancellationToken>()), Times.Once);
			crmMock.Verify(c => c.DeleteAsync("plugintype", TypeId, It.IsAny<CancellationToken>()), Times.Once);
		}

		// ── Fail — delete throws exception ─────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDeleteThrows()
		{
			var (output, repoMock, crmMock, ptRepo, stepRepo, imageRepo) = CreateMocks();

			crmMock
				.Setup(c => c.DeleteAsync("plugintype", TypeId, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new Exception("Simulated delete failure"));

			var executor = CreateExecutor(output, repoMock, ptRepo, stepRepo, imageRepo);
			var result = await executor.ExecuteAsync(
				new UnregisterCommand { PluginTypeName = TypeName },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
		}
	}
}
