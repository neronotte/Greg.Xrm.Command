using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class BindCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly BindCustomApiCommandExecutor executor;
		private static readonly Guid ApiId = Guid.NewGuid();
		private static readonly Guid PluginTypeId = Guid.NewGuid();

		public BindCustomApiCommandExecutorTest()
		{
			this.executor = new BindCustomApiCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		private void SetupApiFound()
		{
			var api = new Entity("customapi") { Id = ApiId };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { api }));
		}

		private void SetupSinglePluginTypeFound()
		{
			var pluginType = new Entity("plugintype") { Id = PluginTypeId };
			pluginType["typename"] = "MyNs.GregPlugin";
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { pluginType }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { pluginType }));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiAndPluginExist()
		{
			SetupApiFound();
			SetupSinglePluginTypeFound();
			this.OrganizationServiceMock
				.Setup(x => x.UpdateAsync(It.IsAny<Entity>()))
				.Returns(Task.CompletedTask);

			var result = await executor.ExecuteAsync(
				new BindCustomApiCommand { ApiUniqueName = "nn_GregSum", PluginTypeName = "MyNs.GregPlugin" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			// Verify UpdateAsync was called with the right entity; attribute check omitted because
			// SaveOrUpdateAsync clears entity attributes after calling UpdateAsync.
			this.OrganizationServiceMock.Verify(
				x => x.UpdateAsync(It.Is<Entity>(e =>
					e.LogicalName == "customapi" &&
					e.Id == ApiId)),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenApiNotFound()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>()))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var result = await executor.ExecuteAsync(
				new BindCustomApiCommand { ApiUniqueName = "nn_Missing", PluginTypeName = "MyNs.GregPlugin" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenPluginTypeNotFound()
		{
			SetupApiFound();
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var result = await executor.ExecuteAsync(
				new BindCustomApiCommand { ApiUniqueName = "nn_GregSum", PluginTypeName = "MyNs.Missing" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenPluginTypeIsAmbiguous()
		{
			SetupApiFound();
			var t1 = new Entity("plugintype") { Id = Guid.NewGuid() };
			t1["typename"] = "MyNs.GregPlugin";
			var t2 = new Entity("plugintype") { Id = Guid.NewGuid() };
			t2["typename"] = "MyNs.GregPlugin";
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { t1, t2 }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "plugintype"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { t1, t2 }));

			var result = await executor.ExecuteAsync(
				new BindCustomApiCommand { ApiUniqueName = "nn_GregSum", PluginTypeName = "MyNs.GregPlugin" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "disambiguate");
			this.OrganizationServiceMock.Verify(x => x.UpdateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
		{
			SetupApiFound();
			SetupSinglePluginTypeFound();
			this.OrganizationServiceMock
				.Setup(x => x.UpdateAsync(It.IsAny<Entity>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(
					new OrganizationServiceFault(), "Simulated fault"));

			var result = await executor.ExecuteAsync(
				new BindCustomApiCommand { ApiUniqueName = "nn_GregSum", PluginTypeName = "MyNs.GregPlugin" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
		}
	}
}
