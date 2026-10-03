using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class GetByUserCommandExecutorTest
	{
		private readonly OutputToMemory output = new();
		private readonly Mock<IOrganizationServiceRepository> repoMock = new();
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly Mock<ISystemUserRepository> userRepoMock = new();
		private readonly Guid userId = Guid.NewGuid();

		private GetByUserCommandExecutor CreateExecutor()
		{
			this.repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(this.crmMock.Object);
			this.userRepoMock
				.Setup(r => r.GetByIdAsync(this.crmMock.Object, this.userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new SystemUser(this.userId, @"DOMAIN\john", fullName: "John"));

			return new GetByUserCommandExecutor(
				this.output,
				this.repoMock.Object,
				new SecurityUserResolver(this.userRepoMock.Object),
				new SecurityRoleService());
		}

		private static Entity Role(Guid id, string name)
		{
			var e = new Entity("role", id);
			e["name"] = name;
			e["ismanaged"] = true;
			e["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) { Name = "Root" };
			return e;
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldMergeDirectAndTeamRoles()
		{
			var shared = Guid.NewGuid();
			var direct = new EntityCollection([Role(shared, "Salesperson")]);
			var team = new EntityCollection([Role(shared, "Salesperson"), Role(Guid.NewGuid(), "Basic User")]);
			this.crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase q, CancellationToken _) =>
				{
					var isTeam = ((QueryExpression)q).LinkEntities.Single().LinkToEntityName == "teamroles";
					return Task.FromResult(isTeam ? team : direct);
				});

			var result = await CreateExecutor().ExecuteAsync(new GetByUserCommand { User = this.userId.ToString() }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.userId, result["UserId"]);
			Assert.AreEqual(2, result["Count"]);
			Assert.AreEqual("Basic User, Salesperson", result["Roles"]);
			var text = this.output.ToString();
			StringAssert.Contains(text, "Found 2 roles.");
			StringAssert.Contains(text, "Basic User");
			StringAssert.Contains(text, "Salesperson");
			StringAssert.Contains(text, "Direct, Team");
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldFailOnDataverseFault()
		{
			this.crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "roles boom"));

			var result = await CreateExecutor().ExecuteAsync(new GetByUserCommand { User = this.userId.ToString() }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "roles boom");
		}

		[TestMethod]
		public async Task ExecuteAsyncWithUnknownUserShouldFail()
		{
			var unknown = Guid.NewGuid();

			var result = await CreateExecutor().ExecuteAsync(new GetByUserCommand { User = unknown.ToString() }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, unknown.ToString());
		}
	}
}
