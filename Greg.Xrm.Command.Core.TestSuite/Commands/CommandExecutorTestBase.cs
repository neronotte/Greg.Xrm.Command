using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Moq;

namespace Greg.Xrm.Command.Commands
{
	public abstract class CommandExecutorTestBase
	{
		protected Mock<IOrganizationServiceRepository> OrganizationServiceRepositoryMock { get; }
		protected Mock<IOrganizationServiceAsync2> OrganizationServiceMock { get; }
		protected OutputToMemory Output { get; }

		protected CommandExecutorTestBase()
		{
			this.OrganizationServiceMock = new Mock<IOrganizationServiceAsync2>();
			this.OrganizationServiceRepositoryMock = new Mock<IOrganizationServiceRepository>();
			this.OrganizationServiceRepositoryMock
				.Setup(m => m.GetCurrentConnectionAsync())
				.ReturnsAsync(this.OrganizationServiceMock.Object);

			this.Output = new OutputToMemory();
		}
	}
}
