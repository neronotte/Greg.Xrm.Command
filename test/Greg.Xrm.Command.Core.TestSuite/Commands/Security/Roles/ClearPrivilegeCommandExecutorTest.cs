using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class ClearPrivilegeCommandExecutorTest
	{
		[TestMethod]
		public void AutomaticRegistrationShouldResolveDelegatedExecutor()
		{
			var services = new ServiceCollection();
			services.RegisterCommandExecutors(typeof(ClearPrivilegeCommand).Assembly);
			services.AddSingleton<IOutput>(new OutputToMemory());
			services.AddSingleton(new Mock<IOrganizationServiceRepository>().Object);
			services.AddSingleton(new Mock<ISecurityRoleService>().Object);
			services.AddSingleton(new Mock<IPrivilegeRepository>().Object);
			using var provider = services.BuildServiceProvider();
			Assert.IsInstanceOfType<ClearPrivilegeCommandExecutor>(provider.GetRequiredService<ICommandExecutor<ClearPrivilegeCommand>>());
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task ShouldForwardSelectorsWithExplicitNullLevelAndCancellationToken(bool tableSelector)
		{
			var command = new ClearPrivilegeCommand
			{
				Role = "Salesperson",
				Name = tableSelector ? null : "prvWriteAccount",
				Table = tableSelector ? "Account" : null,
				Privilege = tableSelector ? "Write" : null
			};
			using var source = new CancellationTokenSource();
			var expected = CommandResult.Success();
			var setExecutor = new Mock<ICommandExecutor<SetPrivilegeCommand>>();
			setExecutor.Setup(executor => executor.ExecuteAsync(It.IsAny<SetPrivilegeCommand>(), source.Token)).ReturnsAsync(expected);
			var executor = new ClearPrivilegeCommandExecutor(setExecutor.Object);

			var result = await executor.ExecuteAsync(command, source.Token);

			Assert.AreSame(expected, result);
			setExecutor.Verify(executor => executor.ExecuteAsync(It.Is<SetPrivilegeCommand>(forwarded =>
				forwarded.Role == command.Role && forwarded.Name == command.Name &&
				forwarded.Table == command.Table && forwarded.Privilege == command.Privilege &&
				forwarded.Level == null && forwarded.LevelSpecified), source.Token), Times.Once);
		}

		[TestMethod]
		public async Task ShouldReturnDelegatedFailureUnchanged()
		{
			var expected = CommandResult.Fail("Privilege not found");
			var setExecutor = new Mock<ICommandExecutor<SetPrivilegeCommand>>();
			setExecutor.Setup(executor => executor.ExecuteAsync(It.IsAny<SetPrivilegeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
			var result = await new ClearPrivilegeCommandExecutor(setExecutor.Object).ExecuteAsync(new ClearPrivilegeCommand { Role = "Salesperson", Name = "prvInvalid" }, CancellationToken.None);
			Assert.AreSame(expected, result);
		}

		[TestMethod]
		public async Task ManagedRoleShouldFailWithoutRemovingPrivilege()
		{
			var crm = new Mock<IOrganizationServiceAsync2>();
			var connections = new Mock<IOrganizationServiceRepository>();
			connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
			crm.Setup(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection([new Entity("role", Guid.NewGuid()) { ["name"] = "Salesperson", ["ismanaged"] = true }]));
			var executor = new ClearPrivilegeCommandExecutor(new SetPrivilegeCommandExecutor(new OutputToMemory(), connections.Object, new SecurityRoleService(), new Privilege.Repository()));

			var result = await executor.ExecuteAsync(new ClearPrivilegeCommand { Role = "Salesperson", Name = "prvWriteAccount" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "managed");
			crm.Verify(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()), Times.Once);
			crm.Verify(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task ShouldRemoveOnlySelectedPrivilegeThroughSetExecutor(bool tableSelector)
		{
			var roleId = Guid.NewGuid();
			var privilegeId = Guid.NewGuid();
			var crm = new Mock<IOrganizationServiceAsync2>();
			var connections = new Mock<IOrganizationServiceRepository>();
			connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
			crm.Setup(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) => Task.FromResult(((QueryExpression)query).EntityName == "role"
					? new EntityCollection([new Entity("role", roleId) { ["name"] = "Salesperson" }])
					: new EntityCollection([new Entity("privilege", privilegeId) { ["name"] = "prvWriteAccount" }])));
			crm.Setup(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new OrganizationResponse());
			var executor = new ClearPrivilegeCommandExecutor(new SetPrivilegeCommandExecutor(new OutputToMemory(), connections.Object, new SecurityRoleService(), new Privilege.Repository()));
			var command = new ClearPrivilegeCommand
			{
				Role = "Salesperson",
				Name = tableSelector ? null : "prvWriteAccount",
				Table = tableSelector ? "Account" : null,
				Privilege = tableSelector ? "Write" : null
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(true, result["Removed"]);
			Assert.AreEqual("None", result["Level"]);
			crm.Verify(service => service.ExecuteAsync(It.Is<OrganizationRequest>(request => request is RemovePrivilegeRoleRequest &&
				((RemovePrivilegeRoleRequest)request).RoleId == roleId && ((RemovePrivilegeRoleRequest)request).PrivilegeId == privilegeId), It.IsAny<CancellationToken>()), Times.Once);
			crm.Verify(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
			crm.Verify(service => service.RetrieveMultipleAsync(It.Is<QueryBase>(query => ((QueryExpression)query).EntityName == "privilege" &&
				Equals(((QueryExpression)query).Criteria.Conditions[0].Values[0], "prvWriteAccount")), It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}