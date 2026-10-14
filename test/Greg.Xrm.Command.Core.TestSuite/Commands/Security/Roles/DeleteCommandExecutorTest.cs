using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class DeleteCommandExecutorTest
	{
		private readonly Guid roleId = Guid.NewGuid();
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly Mock<IOrganizationServiceRepository> connections = new();
		private readonly OutputToMemory output = new();
		private EntityCollection roles = new();
		private readonly List<QueryExpression> queries = [];
		private DeleteCommandExecutor executor = null!;

		[TestInitialize]
		public void Initialize()
		{
			this.roles = new EntityCollection([new Entity("role", this.roleId)
			{
				["name"] = "Salesperson - Copy", ["ismanaged"] = false
			}]);
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(this.crm.Object);
			this.crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					this.queries.Add((QueryExpression)query);
					return Task.FromResult(this.roles);
				});
			this.crm.Setup(client => client.DeleteAsync("role", this.roleId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			this.executor = new DeleteCommandExecutor(this.output, this.connections.Object, new SecurityRole.Repository());
		}

		private DeleteCommand Command(bool useGuid = false) => new() { Role = useGuid ? this.roleId.ToString() : " Salesperson - Copy " };

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task UnmanagedRoleShouldBeDeletedByNameOrGuid(bool useGuid)
		{
			var result = await this.executor.ExecuteAsync(this.Command(useGuid), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.roleId, result["RoleId"]);
			Assert.AreEqual("Salesperson - Copy", result["RoleName"]);
			Assert.AreEqual(true, result["Deleted"]);
			this.crm.Verify(client => client.DeleteAsync("role", this.roleId, CancellationToken.None), Times.Once);
			var query = this.queries.Single();
			CollectionAssert.Contains(query.ColumnSet.Columns.ToArray(), "ismanaged");
			Assert.AreEqual(2, query.TopCount);
			if (useGuid)
			{
				var condition = query.Criteria.Conditions.Single();
				Assert.AreEqual("roleid", condition.AttributeName);
				Assert.AreEqual(this.roleId, condition.Values[0]);
			}
			else
			{
				Assert.IsTrue(query.Criteria.Conditions.Any(condition => condition.AttributeName == "parentroleid" && condition.Operator == ConditionOperator.Null));
				Assert.AreEqual("Salesperson - Copy", query.Criteria.Conditions.Single(condition => condition.AttributeName == "name").Values[0]);
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task ManagedRoleShouldNeverBeDeleted(bool useGuid)
		{
			this.roles.Entities[0]["ismanaged"] = true;
			var result = await this.executor.ExecuteAsync(this.Command(useGuid), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "is managed");
			StringAssert.Contains(result.ErrorMessage, "Only unmanaged");
			this.crm.Verify(client => client.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task UnknownManagedStatusShouldNeverBeDeleted()
		{
			this.roles.Entities[0].Attributes.Remove("ismanaged");
			var result = await this.executor.ExecuteAsync(this.Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Unable to determine");
			this.crm.Verify(client => client.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task MissingOrAmbiguousRoleShouldFailWithoutDeleting(bool ambiguous)
		{
			if (ambiguous) this.roles.Entities.Add(new Entity("role", Guid.NewGuid()) { ["name"] = "Salesperson - Copy", ["ismanaged"] = false });
			else this.roles.Entities.Clear();
			var result = await this.executor.ExecuteAsync(this.Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, ambiguous ? "GUID" : "not found");
			this.crm.Verify(client => client.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task ReadOrDeleteFaultShouldReturnFailure(bool deleteFault)
		{
			var fault = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Deletion blocked");
			if (deleteFault)
				this.crm.Setup(client => client.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(fault);
			else
				this.crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>())).ThrowsAsync(fault);
			var result = await this.executor.ExecuteAsync(this.Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Deletion blocked");
			if (!deleteFault) this.crm.Verify(client => client.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task CancellationShouldPropagateBeforeConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => this.executor.ExecuteAsync(this.Command(), cancellation.Token));
			this.connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task CancellationTokenShouldReachLookupAndDelete()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await this.executor.ExecuteAsync(this.Command(), cancellation.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.crm.Verify(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token), Times.Once);
			this.crm.Verify(client => client.DeleteAsync("role", this.roleId, cancellation.Token), Times.Once);
		}

		[TestMethod]
		public async Task CancellationDuringDeleteShouldPropagate()
		{
			using var cancellation = new CancellationTokenSource();
			this.crm.Setup(client => client.DeleteAsync("role", this.roleId, cancellation.Token))
				.Returns(() =>
				{
					cancellation.Cancel();
					return Task.FromCanceled(cancellation.Token);
				});
			await Assert.ThrowsAsync<OperationCanceledException>(() => this.executor.ExecuteAsync(this.Command(), cancellation.Token));
		}

		[TestMethod]
		public void ExecutorShouldResolveUsingCoreModule()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new IoCModule());
			builder.RegisterInstance(this.connections.Object).As<IOrganizationServiceRepository>();
			builder.RegisterInstance(this.output).As<IOutput>();
			builder.RegisterType<DeleteCommandExecutor>().As<ICommandExecutor<DeleteCommand>>();
			using var container = builder.Build();
			Assert.IsInstanceOfType<DeleteCommandExecutor>(container.Resolve<ICommandExecutor<DeleteCommand>>());
		}
	}
}