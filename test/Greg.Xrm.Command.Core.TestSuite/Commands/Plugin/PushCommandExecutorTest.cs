using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[TestClass]
	public class PushCommandExecutorTest : CommandExecutorTestBase
	{
		private const string SolutionName = "test_solution";
		private const int PluginPackageObjectTypeCode = 10123;

		private readonly Mock<ISolutionRepository> solutionRepositoryMock = new();
		private readonly Mock<IPluginPackageReader> pluginPackageReaderMock = new();
		private readonly Mock<IPluginPackageRepository> pluginPackageRepositoryMock = new();
		private readonly Mock<IPluginAssemblyRepository> pluginAssemblyRepositoryMock = new();
		private readonly Mock<IPluginTypeRepository> pluginTypeRepositoryMock = new();
		private readonly Mock<IObjectTypeCodeFinder> objectTypeCodeFinderMock = new();

		public PushCommandExecutorTest()
		{
			var solutionEntity = new Entity("solution")
			{
				Id = Guid.NewGuid(),
				["ismanaged"] = false,
				["uniquename"] = SolutionName,
				["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", "test")
			};
			var solution = (Model.Solution)Activator.CreateInstance(
				typeof(Model.Solution),
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
				null,
				[solutionEntity],
				null)!;

			this.solutionRepositoryMock
				.Setup(x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), SolutionName))
				.ReturnsAsync(solution);
			this.pluginPackageReaderMock
				.Setup(x => x.ReadPackageFile(It.IsAny<string>()))
				.Returns(PluginPackageReadResult.Success("Package", "1.0.0", "content"));
			this.pluginPackageRepositoryMock
				.Setup(x => x.GetByIdAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					"test_Package",
					It.IsAny<CancellationToken>()))
				.ReturnsAsync((PluginPackage?)null);
			this.pluginAssemblyRepositoryMock
				.Setup(x => x.GetByPackageIdAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					It.IsAny<Guid>(),
					It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
			this.objectTypeCodeFinderMock
				.Setup(x => x.GetObjectTypeCodeForTableAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					"pluginpackage",
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(PluginPackageObjectTypeCode);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldAddNewPackageToSolutionUsingDynamicObjectTypeCode()
		{
			var packageId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "pluginpackage")))
				.ReturnsAsync(packageId);
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new AddSolutionComponentResponse());

			var executor = CreateExecutor();

			var result = await executor.ExecuteAsync(
				new PushCommand { Path = "Package.nupkg", SolutionName = SolutionName },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.objectTypeCodeFinderMock.Verify(
				x => x.GetObjectTypeCodeForTableAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					"pluginpackage",
					It.IsAny<CancellationToken>()),
				Times.Once);
			this.OrganizationServiceMock.Verify(
				x => x.ExecuteAsync(It.Is<AddSolutionComponentRequest>(request =>
					request.ComponentId == packageId &&
					request.ComponentType == PluginPackageObjectTypeCode &&
					request.SolutionUniqueName == SolutionName), It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldNotAddExistingPackageToSolution()
		{
			var package = new PluginPackage { ismanaged = false };
			((IEntityWrapperInternal)package).SetId(Guid.NewGuid());
			this.pluginPackageRepositoryMock
				.Setup(x => x.GetByIdAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					"test_Package",
					It.IsAny<CancellationToken>()))
				.ReturnsAsync(package);

			var result = await CreateExecutor().ExecuteAsync(
				new PushCommand { Path = "Package.nupkg", SolutionName = SolutionName },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.objectTypeCodeFinderMock.Verify(
				x => x.GetObjectTypeCodeForTableAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					"pluginpackage",
					It.IsAny<CancellationToken>()),
				Times.Never);
			this.OrganizationServiceMock.Verify(
				x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()),
				Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFailBeforeCreatingPackage_WhenObjectTypeCodeLookupFails()
		{
			this.objectTypeCodeFinderMock
				.Setup(x => x.GetObjectTypeCodeForTableAsync(
					It.IsAny<IOrganizationServiceAsync2>(),
					"pluginpackage",
					It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(),
					"Lookup failed"));

			var result = await CreateExecutor().ExecuteAsync(
				new PushCommand { Path = "Package.nupkg", SolutionName = SolutionName },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Lookup failed");
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.IsAny<Entity>()),
				Times.Never);
			this.OrganizationServiceMock.Verify(
				x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()),
				Times.Never);
		}

		private PushCommandExecutor CreateExecutor()
		{
			return new PushCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
				this.solutionRepositoryMock.Object,
				this.pluginPackageReaderMock.Object,
				this.pluginPackageRepositoryMock.Object,
				this.pluginAssemblyRepositoryMock.Object,
				this.pluginTypeRepositoryMock.Object,
				this.objectTypeCodeFinderMock.Object);
		}
	}
}
