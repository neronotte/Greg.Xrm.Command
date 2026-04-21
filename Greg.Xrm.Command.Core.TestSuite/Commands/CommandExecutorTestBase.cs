using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.Net.Http;

namespace Greg.Xrm.Command.Commands
{
	public abstract class CommandExecutorTestBase
	{
		protected Mock<IOrganizationServiceRepository> OrganizationServiceRepositoryMock { get; }
		protected Mock<IOrganizationServiceAsync2> OrganizationServiceMock { get; }
		protected Mock<ITokenProvider> TokenProviderMock { get; }
		protected Mock<IHttpClientFactory> HttpClientFactoryMock { get; }
		protected OutputToMemory Output { get; }

		protected CommandExecutorTestBase()
		{
			this.OrganizationServiceMock = new Mock<IOrganizationServiceAsync2>();
			this.OrganizationServiceRepositoryMock = new Mock<IOrganizationServiceRepository>();
			this.OrganizationServiceRepositoryMock
				.Setup(m => m.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(this.OrganizationServiceMock.Object);

			this.TokenProviderMock = new Mock<ITokenProvider>();
			this.HttpClientFactoryMock = new Mock<IHttpClientFactory>();

			this.Output = new OutputToMemory();
		}
	}
}
