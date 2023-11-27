
using Greg.Xrm.Command.Commands.UnifiedRouting;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class GetAgentStatusCommandExecutorTest
	{
		[TestMethod]
        [TestCategory("Integration")]
        public void Test1()
		{
			var agentPrimaryEmail = "francesco.catino@external.eniplenitude.com";
			OrganizationRequest? requestToServer = null;

			var output = new OutputToMemory();

			var organizationServiceRepository = new Mock<IOrganizationServiceRepository>();
			var organizationService = new Mock<IOrganizationServiceAsync2>();
			organizationService.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.Callback<OrganizationRequest>(x => requestToServer = x);

			organizationServiceRepository
				.Setup(organizationServiceRepository => organizationServiceRepository.GetCurrentConnectionAsync())
				.ReturnsAsync(organizationService.Object);

			var executor = new GetAgentStatusCommandExecutor(output, organizationServiceRepository.Object);

			executor.ExecuteAsync(new GetAgentStatusCommand
            {
				AgentPrimaryEmail = agentPrimaryEmail
            }, new CancellationToken()).Wait();

			organizationServiceRepository.Verify(x => x.GetCurrentConnectionAsync(), Times.Once);
		}
	}
}
