using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class ListCommandTest
	{
		[TestMethod]
		public void ParseWithoutUnmanagedOnlyShouldDefaultToFalse()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list");

			Assert.IsFalse(command.UnmanagedOnly);
		}

		[TestMethod]
		public void ParseWithUnmanagedOnlyShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list", "--unmanaged-only");

			Assert.IsTrue(command.UnmanagedOnly);
		}

		[TestMethod]
		public void ParseWithShortNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list", "-um");

			Assert.IsTrue(command.UnmanagedOnly);
		}

		[TestMethod]
		public void ParseWithNameShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list", "--name", "sales");

			Assert.AreEqual("sales", command.Name);
		}

		[TestMethod]
		public void ParseWithNameShortShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list", "-n", "sales");

			Assert.AreEqual("sales", command.Name);
		}

		[TestMethod]
		public void ParseWithNameAndUnmanagedOnlyShouldWork()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list", "-n", "sales", "-um");

			Assert.AreEqual("sales", command.Name);
			Assert.IsTrue(command.UnmanagedOnly);
		}

		[TestMethod]
		public void ParseWithoutNameShouldBeNull()
		{
			var command = Utility.TestParseCommand<ListCommand>("security", "roles", "list");

			Assert.IsNull(command.Name);
		}

		private static (ListCommandExecutor Executor, OutputToMemory Output, List<QueryExpression> Queries) CreateExecutor(params Entity[] roles)
		{
			var queries = new List<QueryExpression>();
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase q, CancellationToken _) =>
				{
					queries.Add((QueryExpression)q);
					return Task.FromResult(new EntityCollection([.. roles]));
				});
			var repoMock = new Mock<IOrganizationServiceRepository>();
			repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);

			var output = new OutputToMemory();
			return (new ListCommandExecutor(output, repoMock.Object, new SecurityRoleService()), output, queries);
		}

		private static Entity Role(string name)
		{
			var e = new Entity("role", Guid.NewGuid());
			e["name"] = name;
			e["ismanaged"] = false;
			e["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) { Name = "Root" };
			return e;
		}

		[TestMethod]
		public async Task ExecuteAsyncWithNameShouldFilterRolesByName()
		{
			var (executor, output, queries) = CreateExecutor(Role("Sales Manager"));

			var result = await executor.ExecuteAsync(new ListCommand { Name = "sales" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var condition = queries.Single().Criteria.Conditions.Single(c => c.AttributeName == "name");
			Assert.AreEqual(ConditionOperator.Like, condition.Operator);
			Assert.AreEqual("%sales%", condition.Values[0]);
			StringAssert.Contains(output.ToString(), "Sales Manager");
		}

		[TestMethod]
		public async Task ExecuteAsyncWithoutNameShouldNotFilterRolesByName()
		{
			var (executor, _, queries) = CreateExecutor();

			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsFalse(queries.Single().Criteria.Conditions.Any(c => c.AttributeName == "name"));
			Assert.AreEqual(0, result["Count"]);
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldReturnRolesInResult()
		{
			var (executor, output, _) = CreateExecutor(Role("Sales Manager"));

			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(1, result["Count"]);
			Assert.AreEqual("Sales Manager", result["Roles"]);
			Assert.IsFalse(result.Values.Any(value => value is System.Collections.IEnumerable && value is not string));
			StringAssert.Contains(output.ToString(), "Found 1 role.");
		}

		[TestMethod]
		public async Task ExecuteAsyncShouldFailOnDataverseFault()
		{
			var crmMock = new Mock<IOrganizationServiceAsync2>();
			crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new System.ServiceModel.FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "list boom"));
			var repoMock = new Mock<IOrganizationServiceRepository>();
			repoMock.Setup(r => r.GetCurrentConnectionAsync()).ReturnsAsync(crmMock.Object);
			var executor = new ListCommandExecutor(new OutputToMemory(), repoMock.Object, new SecurityRoleService());

			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "list boom");
		}
	}
}
