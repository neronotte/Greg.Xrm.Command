using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
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
				new SecurityPrivilegeService(new Organization.Repository(), new BusinessUnit.Repository()));
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
		[DataRow(false)]
		[DataRow(true)]
		public async Task TableOutputShouldShowBusinessUnitsOnlyWhenEnabled(bool enabled)
		{
			var executor = CreateExecutor();
			var privilegeId = Guid.NewGuid();
			var europe = Guid.NewGuid();
			var america = Guid.NewGuid();
			var privilege = (SecurityPrivilegeMetadata)Activator.CreateInstance(typeof(SecurityPrivilegeMetadata), nonPublic: true)!;
			typeof(SecurityPrivilegeMetadata).GetProperty(nameof(SecurityPrivilegeMetadata.PrivilegeId))!.SetValue(privilege, privilegeId);
			typeof(SecurityPrivilegeMetadata).GetProperty(nameof(SecurityPrivilegeMetadata.PrivilegeType))!.SetValue(privilege, PrivilegeType.Read);
			var table = new EntityMetadata { LogicalName = "account" };
			typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.Privileges))!.SetValue(table, new[] { privilege });
			this.crmMock.Setup(client => client.ExecuteAsync(It.IsAny<RetrieveEntityRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new RetrieveEntityResponse { Results = { ["EntityMetadata"] = table } });
			this.crmMock.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) => Task.FromResult(((QueryExpression)query).EntityName == "organization"
					? new EntityCollection([new Entity("organization", Guid.NewGuid())
					{
						["orgdborgsettings"] = $"<OrgSettings><EnableOwnershipAcrossBusinessUnits>{enabled.ToString().ToLowerInvariant()}</EnableOwnershipAcrossBusinessUnits></OrgSettings>"
					}])
					: new EntityCollection([
						new Entity("businessunit", europe) { ["name"] = "Europe" },
						new Entity("businessunit", america) { ["name"] = "America" }
					])));
			this.crmMock.Setup(client => client.ExecuteAsync(It.IsAny<RetrieveUserPrivilegesRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new RetrieveUserPrivilegesResponse
				{
					Results = { ["RolePrivileges"] = new[]
					{
						new RolePrivilege { PrivilegeId = privilegeId, BusinessUnitId = europe, Depth = PrivilegeDepth.Local },
						new RolePrivilege { PrivilegeId = privilegeId, BusinessUnitId = america, Depth = PrivilegeDepth.Deep }
					} }
				});

			var result = await executor.ExecuteAsync(new CheckPrivilegeCommand { User = this.userId.ToString(), TableName = "account" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(enabled ? 2 : 1, result["Count"]);
			if (enabled)
			{
				StringAssert.Contains(this.output.ToString(), "Business Unit");
				StringAssert.Contains(this.output.ToString(), $"Europe ({europe})");
				StringAssert.Contains(this.output.ToString(), $"America ({america})");
				StringAssert.Contains((string)result["Privileges"], $"Read (Local) [BU: Europe ({europe})]");
				StringAssert.Contains((string)result["Privileges"], $"Read (Deep) [BU: America ({america})]");
			}
			else
			{
				Assert.IsFalse(this.output.ToString().Contains("Business Unit"));
				Assert.AreEqual("Read (Deep)", result["Privileges"]);
			}
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
