using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class CheckPrivilegeCommandExecutorTest
	{
		private readonly OutputToMemory output = new();
		private readonly Mock<IOrganizationServiceRepository> repoMock = new();
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly Mock<ISystemUserRepository> userRepoMock = new();
		private readonly Guid userId = Guid.NewGuid();

		private CheckPrivilegeCommandExecutor CreateExecutor()
		{
			this.repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(this.crmMock.Object);
			this.userRepoMock
				.Setup(r => r.GetByIdAsync(this.crmMock.Object, this.userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new SystemUser(this.userId, @"DOMAIN\john", fullName: "John"));
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrieveEntityRequest>(), It.IsAny<CancellationToken>()))
				.Returns(() =>
				{
					var response = new RetrieveEntityResponse();
					response.Results["EntityMetadata"] = new EntityMetadata { LogicalName = "account" };
					return Task.FromResult<OrganizationResponse>(response);
				});

			return new CheckPrivilegeCommandExecutor(
				this.output,
				this.repoMock.Object,
				new SecurityUserResolver(this.userRepoMock.Object),
				new SecurityPrivilegeService());
		}

		private void SetupPrincipalAccess(AccessRights rights)
		{
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrievePrincipalAccessRequest>(), It.IsAny<CancellationToken>()))
				.Returns(() =>
				{
					var response = new RetrievePrincipalAccessResponse();
					response.Results["AccessRights"] = rights;
					return Task.FromResult<OrganizationResponse>(response);
				});
		}

		[TestMethod]
		public async Task ExecuteAsyncOnRecordShouldReturnAccessRightsInResult()
		{
			var executor = CreateExecutor();
			SetupPrincipalAccess(AccessRights.ReadAccess | AccessRights.WriteAccess);

			var command = new CheckPrivilegeCommand { User = this.userId.ToString(), TableName = "account", RecordId = Guid.NewGuid().ToString() };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.userId, result["UserId"]);
			Assert.AreEqual(2, result["Count"]);
			CollectionAssert.AreEqual(
				new[] { "Read", "Write" },
				((IEnumerable<SecurityPrivilegeInfo>)result["Privileges"]).Select(x => x.Privilege).ToArray());
			StringAssert.Contains(this.output.ToString(), "Found 2 privileges.");
		}

		[TestMethod]
		public async Task ExecuteAsyncWithSinglePrivilegeShouldUseSingularWording()
		{
			var executor = CreateExecutor();
			SetupPrincipalAccess(AccessRights.ReadAccess);

			var command = new CheckPrivilegeCommand { User = this.userId.ToString(), TableName = "account", RecordId = Guid.NewGuid().ToString() };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.output.ToString(), "Found 1 privilege.");
		}

		[TestMethod]
		public async Task ExecuteAsyncWithInvalidRecordIdShouldFailWithoutConnecting()
		{
			var executor = CreateExecutor();

			var result = await executor.ExecuteAsync(new CheckPrivilegeCommand { TableName = "account", RecordId = "not-a-guid" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not-a-guid");
			this.repoMock.Verify(r => r.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldFailOnDataverseFault()
		{
			var executor = CreateExecutor();
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrievePrincipalAccessRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "access boom"));

			var command = new CheckPrivilegeCommand { User = this.userId.ToString(), TableName = "account", RecordId = Guid.NewGuid().ToString() };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "access boom");
		}

		[TestMethod]
		public async Task ExecuteAsyncWithUnknownUserShouldFail()
		{
			var executor = CreateExecutor();
			this.userRepoMock
				.Setup(r => r.GetByDomainNameOrEmailAsync(this.crmMock.Object, "ghost", 2, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var result = await executor.ExecuteAsync(new CheckPrivilegeCommand { User = "ghost", TableName = "account" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "ghost");
		}
	}
}
