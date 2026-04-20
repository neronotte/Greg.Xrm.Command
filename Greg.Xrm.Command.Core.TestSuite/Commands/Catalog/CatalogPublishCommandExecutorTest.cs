using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Catalog
{
	[TestClass]
	public class CatalogPublishCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CatalogPublishCommandExecutor executor;

		public CatalogPublishCommandExecutorTest()
		{
			this.executor = new CatalogPublishCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithBasicInputs_ShouldCreateCatalogItem()
		{
			var expectedItemId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "catalogitem")))
				.ReturnsAsync(expectedItemId);

			var command = new CatalogPublishCommand
			{
				Name = "new_MyBusinessEvent",
				Type = "BusinessEvent",
				Description = "Test business event"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "catalogitem" &&
				e["uniquename"].ToString() == "new_MyBusinessEvent" &&
				e["displayname"].ToString() == "new_MyBusinessEvent" &&
				e["description"].ToString() == "Test business event" &&
				((OptionSetValue)e["catalogitemtype"]).Value == 1
			)), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithDryRun_ShouldNotCreate()
		{
			var command = new CatalogPublishCommand
			{
				Name = "new_MyItem",
				DryRun = true
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.IsAny<Entity>()), Times.Never);
			StringAssert.Contains(this.Output.ToString(), "DRY RUN");
		}

		[TestMethod]
		public async Task ExecuteAsync_WithVersion_ShouldSetVersion()
		{
			var expectedItemId = Guid.NewGuid();
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
				.ReturnsAsync(expectedItemId);

			var command = new CatalogPublishCommand
			{
				Name = "new_MyItem",
				Version = "2.0.0"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
		}
	}
}
