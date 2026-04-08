using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[TestClass]
	public class ListCommandExecutorTest
	{
		// ── Helpers ───────────────────────────────────────────────────────────

		private static (
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> orgRepoMock,
			Mock<ISolutionRepository> solutionRepoMock,
			Mock<IAnsiConsole> ansiConsoleMock,
			Mock<IPluginAssemblyRepository> assemblyRepoMock,
			Mock<IPluginPackageRepository> packageRepoMock,
			Mock<IPluginTypeRepository> typeRepoMock,
			Mock<ISdkMessageProcessingStepRepository> stepRepoMock,
			Mock<ISdkMessageProcessingStepImageRepository> imageRepoMock)
		CreateMocks()
		{
			var output = new OutputToMemory();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			var orgRepoMock = new Mock<IOrganizationServiceRepository>();
			orgRepoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);

			var solutionRepoMock = new Mock<ISolutionRepository>();

			var ansiConsoleMock = new Mock<IAnsiConsole>();
			ansiConsoleMock.Setup(c => c.Write(It.IsAny<IRenderable>()));

			var assemblyRepoMock = new Mock<IPluginAssemblyRepository>();
			SetupEmptySearch(assemblyRepoMock);

			var packageRepoMock = new Mock<IPluginPackageRepository>();
			SetupEmptySearch(packageRepoMock);

			var typeRepoMock = new Mock<IPluginTypeRepository>();
			SetupEmptySearch(typeRepoMock);
			typeRepoMock
				.Setup(r => r.GetByAssemblyId(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var stepRepoMock = new Mock<ISdkMessageProcessingStepRepository>();
			stepRepoMock
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
			stepRepoMock
				.Setup(r => r.GetByAssemblyIdAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var imageRepoMock = new Mock<ISdkMessageProcessingStepImageRepository>();
			SetupEmptySearch(imageRepoMock);
			imageRepoMock
				.Setup(r => r.GetByStepIdsAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<Guid[]>()))
				.ReturnsAsync([]);

			return (output, orgRepoMock, solutionRepoMock, ansiConsoleMock, assemblyRepoMock, packageRepoMock, typeRepoMock, stepRepoMock, imageRepoMock);
		}

		private static void SetupEmptySearch(Mock<IPluginAssemblyRepository> mock) =>
			mock.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

		private static void SetupEmptySearch(Mock<IPluginPackageRepository> mock) =>
			mock.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

		private static void SetupEmptySearch(Mock<IPluginTypeRepository> mock) =>
			mock.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

		private static void SetupEmptySearch(Mock<ISdkMessageProcessingStepImageRepository> mock) =>
			mock.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

		private static ListCommandExecutor CreateExecutor(
			OutputToMemory output,
			Mock<IOrganizationServiceRepository> orgRepoMock,
			Mock<ISolutionRepository> solutionRepoMock,
			Mock<IAnsiConsole> ansiConsoleMock,
			Mock<IPluginAssemblyRepository> assemblyRepoMock,
			Mock<IPluginPackageRepository> packageRepoMock,
			Mock<IPluginTypeRepository> typeRepoMock,
			Mock<ISdkMessageProcessingStepRepository> stepRepoMock,
			Mock<ISdkMessageProcessingStepImageRepository> imageRepoMock)
		{
			return new ListCommandExecutor(
				output,
				orgRepoMock.Object,
				solutionRepoMock.Object,
				ansiConsoleMock.Object,
				assemblyRepoMock.Object,
				packageRepoMock.Object,
				typeRepoMock.Object,
				stepRepoMock.Object,
				imageRepoMock.Object);
		}

		private static PluginAssembly BuildAssembly(Guid id, string name)
		{
			var asm = new PluginAssembly { name = name };
			((Greg.Xrm.Command.Model.IEntityWrapperInternal)asm).SetId(id);
			return asm;
		}

		private static PluginPackage BuildPackage(Guid id, string name)
		{
			var pkg = new PluginPackage { name = name };
			((Greg.Xrm.Command.Model.IEntityWrapperInternal)pkg).SetId(id);
			return pkg;
		}


		// ── No results ────────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_NoResults_ShouldSucceedAndPrintMessage()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();
			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);

			var result = await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(output.ToString(), "No plugin registrations found");
			ansi.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.Never);
		}


		// ── Assembly match ────────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_AssemblyMatchFound_ShouldSucceedAndRenderTree()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();

			var assemblyId = Guid.NewGuid();
			var fakeAssembly = BuildAssembly(assemblyId, "Contoso.Plugins");

			asmRepo
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([fakeAssembly]);

			// hierarchy queries for the assembly
			typeRepo
				.Setup(r => r.GetByAssemblyId(It.IsAny<IOrganizationServiceAsync2>(), assemblyId, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
			stepRepo
				.Setup(r => r.GetByAssemblyIdAsync(It.IsAny<IOrganizationServiceAsync2>(), assemblyId, false, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);

			var result = await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			ansi.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.Once);
		}


		// ── Search operator ───────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_NameWithoutStar_ShouldUseContainsOperator()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();

			ConditionOperator capturedOp = ConditionOperator.Equal;
			asmRepo
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.Callback<IOrganizationServiceAsync2, string, ConditionOperator, CancellationToken>((_, _, op, _) => capturedOp = op)
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);
			await executor.ExecuteAsync(new ListCommand { Name = "Contoso" }, CancellationToken.None);

			Assert.AreEqual(ConditionOperator.Like, capturedOp);
		}

		[TestMethod]
		public async Task ExecuteAsync_NameWithTrailingStar_ShouldStripStarAndUseBeginsWithOperator()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();

			string capturedTerm = string.Empty;
			ConditionOperator capturedOp = ConditionOperator.Equal;
			asmRepo
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.Callback<IOrganizationServiceAsync2, string, ConditionOperator, CancellationToken>((_, term, op, _) => { capturedTerm = term; capturedOp = op; })
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);
			await executor.ExecuteAsync(new ListCommand { Name = "Contoso*" }, CancellationToken.None);

			Assert.AreEqual("Contoso", capturedTerm);
			Assert.AreEqual(ConditionOperator.BeginsWith, capturedOp);
		}


		// ── Level filtering ───────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_LevelAssembly_ShouldOnlySearchAssemblies()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();
			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);

			await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso", Level = SearchLevel.Assembly },
				CancellationToken.None);

			asmRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Once);
			pkgRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Never);
			typeRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Never);
			stepRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
			imgRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_LevelPackage_ShouldOnlySearchPackages()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();
			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);

			await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso", Level = SearchLevel.Package },
				CancellationToken.None);

			pkgRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Once);
			asmRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Never);
			typeRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Never);
			stepRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
			imgRepo.Verify(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()), Times.Never);
		}


		// ── Package match triggers assembly load ──────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_PackageMatchFound_ShouldCallGetByPackageIdAsync()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();

			var packageId = Guid.NewGuid();
			var fakePackage = BuildPackage(packageId, "Contoso.Package");

			pkgRepo
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([fakePackage]);

			asmRepo
				.Setup(r => r.GetByPackageIdAsync(It.IsAny<IOrganizationServiceAsync2>(), packageId, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);

			var result = await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			asmRepo.Verify(r => r.GetByPackageIdAsync(It.IsAny<IOrganizationServiceAsync2>(), packageId, It.IsAny<CancellationToken>()), Times.Once);
		}


		// ── --solution filter ─────────────────────────────────────────────────

		[TestMethod]
		public async Task ExecuteAsync_SolutionNotFound_ShouldFail()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();

			solutionRepo
				.Setup(r => r.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), "MissingSolution"))
				.ReturnsAsync((Greg.Xrm.Command.Model.Solution?)null);

			// Assembly search returns a result to pass the first empty-check
			var assemblyId = Guid.NewGuid();
			asmRepo
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([BuildAssembly(assemblyId, "Contoso.Plugins")]);

			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);
			var result = await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso", SolutionName = "MissingSolution" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "MissingSolution");
			ansi.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_SolutionFilter_ShouldIntersectAssemblies()
		{
			var (output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo) = CreateMocks();

			var solutionId = Guid.NewGuid();
			var entity = new Microsoft.Xrm.Sdk.Entity("solution") { Id = solutionId };
			var fakeSolution = (Greg.Xrm.Command.Model.Solution)Activator.CreateInstance(
				typeof(Greg.Xrm.Command.Model.Solution),
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
				null, [entity], null)!;

			solutionRepo
				.Setup(r => r.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), "MySolution"))
				.ReturnsAsync(fakeSolution);

			// Two assemblies match by name...
			var inSolutionId = Guid.NewGuid();
			var outSolutionId = Guid.NewGuid();
			asmRepo
				.Setup(r => r.SearchByNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>(), It.IsAny<ConditionOperator>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([BuildAssembly(inSolutionId, "Contoso.InSolution"), BuildAssembly(outSolutionId, "Contoso.OutSolution")]);

			// ...but only one is in the solution
			asmRepo
				.Setup(r => r.GetBySolutionIdAsync(It.IsAny<IOrganizationServiceAsync2>(), solutionId, It.IsAny<CancellationToken>()))
				.ReturnsAsync([BuildAssembly(inSolutionId, "Contoso.InSolution")]);

			typeRepo
				.Setup(r => r.GetByAssemblyId(It.IsAny<IOrganizationServiceAsync2>(), inSolutionId, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
			stepRepo
				.Setup(r => r.GetByAssemblyIdAsync(It.IsAny<IOrganizationServiceAsync2>(), inSolutionId, false, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var executor = CreateExecutor(output, orgRepo, solutionRepo, ansi, asmRepo, pkgRepo, typeRepo, stepRepo, imgRepo);
			var result = await executor.ExecuteAsync(
				new ListCommand { Name = "Contoso", SolutionName = "MySolution" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			// Tree should be rendered (the in-solution assembly survived the filter)
			ansi.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.Once);
			// Hierarchy should only have been loaded for the in-solution assembly
			typeRepo.Verify(r => r.GetByAssemblyId(It.IsAny<IOrganizationServiceAsync2>(), outSolutionId, It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
