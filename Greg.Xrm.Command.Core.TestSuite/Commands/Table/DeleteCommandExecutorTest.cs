
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class DeleteCommandExecutorTest
	{
		[TestMethod]
		public void Test1()
		{
			var tableName = "table";
			OrganizationRequest? requestToServer = null;

			var output = new OutputToMemory();

			var organizationServiceRepository = new Mock<IOrganizationServiceRepository>();
			var organizationService = new Mock<IOrganizationServiceAsync2>();
			organizationService.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.Callback<OrganizationRequest>(x => requestToServer = x)
				.ReturnsAsync(new DeleteEntityResponse());

			organizationServiceRepository
				.Setup(organizationServiceRepository => organizationServiceRepository.GetCurrentConnectionAsync())
				.ReturnsAsync(organizationService.Object);

			var executor = new DeleteCommandExecutor(output, organizationServiceRepository.Object);

			executor.ExecuteAsync(new DeleteCommand
			{
				SchemaName = tableName
			}, new CancellationToken()).Wait();

			organizationServiceRepository.Verify(x => x.GetCurrentConnectionAsync(), Times.Once);
			organizationService.Verify(x => x.ExecuteAsync(It.IsAny<DeleteEntityRequest>()), Times.Once);

			Assert.IsNotNull(requestToServer);
			Assert.IsTrue(requestToServer is DeleteEntityRequest);

			var r = (DeleteEntityRequest)requestToServer;
			Assert.IsNotNull(r.LogicalName);
			Assert.AreEqual(tableName, r.LogicalName);
		}
	}
}
