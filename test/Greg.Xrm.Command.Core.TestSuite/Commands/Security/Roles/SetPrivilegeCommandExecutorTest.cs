using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class SetPrivilegeCommandExecutorTest
	{
		private readonly Guid roleId = Guid.NewGuid();
		private readonly Guid privilegeId = Guid.NewGuid();
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly Mock<IOrganizationServiceRepository> connections = new();
		private readonly List<QueryExpression> queries = [];
		private readonly List<OrganizationRequest> requests = [];
		private EntityCollection roles = new();
		private EntityCollection privileges = new();
		private SetPrivilegeCommandExecutor executor = null!;

		[TestInitialize]
		public void Initialize()
		{
			this.roles = new EntityCollection([new Entity("role", this.roleId) { ["name"] = "Salesperson" }]);
			this.privileges = new EntityCollection([new Entity("privilege", this.privilegeId)
			{
				["name"] = "prvWriteAccount",
				["canbebasic"] = true,
				["canbelocal"] = true,
				["canbedeep"] = true,
				["canbeglobal"] = true
			}]);
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(this.crm.Object);
			this.crm.Setup(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) =>
				{
					var expression = (QueryExpression)query;
					this.queries.Add(expression);
					return Task.FromResult(expression.EntityName == "role" ? this.roles : this.privileges);
				});
			this.crm.Setup(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Callback<OrganizationRequest, CancellationToken>((request, _) => this.requests.Add(request))
				.ReturnsAsync(new OrganizationResponse());
			this.executor = new SetPrivilegeCommandExecutor(new OutputToMemory(), this.connections.Object, new SecurityRoleService(), new Privilege.Repository());
		}

		private static SetPrivilegeCommand Command(string? level = "Basic") => new()
		{
			Role = "Salesperson",
			Name = "prvWriteAccount",
			Level = level
		};

		[TestMethod]
		[DataRow("Basic", PrivilegeDepth.Basic)]
		[DataRow("User", PrivilegeDepth.Basic)]
		[DataRow("Local", PrivilegeDepth.Local)]
		[DataRow("BusinessUnit", PrivilegeDepth.Local)]
		[DataRow("Deep", PrivilegeDepth.Deep)]
		[DataRow("ParentChild", PrivilegeDepth.Deep)]
		[DataRow("Global", PrivilegeDepth.Global)]
		[DataRow("Organization", PrivilegeDepth.Global)]
		[DataRow(" gLoBaL ", PrivilegeDepth.Global)]
		[DataRow("1", PrivilegeDepth.Basic)]
		[DataRow("2", PrivilegeDepth.Local)]
		[DataRow("3", PrivilegeDepth.Deep)]
		[DataRow("4", PrivilegeDepth.Global)]
		[DataRow(" 2 ", PrivilegeDepth.Local)]
		public async Task SupportedLevelShouldSetOnlySelectedPrivilege(string level, PrivilegeDepth expected)
		{
			var result = await this.executor.ExecuteAsync(Command(level), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(1, this.requests);
			var request = (AddPrivilegesRoleRequest)this.requests[0];
			Assert.AreEqual(this.roleId, request.RoleId);
			Assert.HasCount(1, request.Privileges);
			Assert.AreEqual(this.privilegeId, request.Privileges[0].PrivilegeId);
			Assert.AreEqual(expected, request.Privileges[0].Depth);
			Assert.AreEqual(expected.ToString(), result["Level"]);
			Assert.AreEqual(false, result["Removed"]);
			Assert.AreEqual(this.privilegeId, result["PrivilegeId"]);
			var roleQuery = this.queries.Single(query => query.EntityName == "role");
			Assert.IsTrue(roleQuery.Criteria.Conditions.Any(condition => condition.AttributeName == "parentroleid" && condition.Operator == ConditionOperator.Null));
			Assert.IsTrue(roleQuery.Criteria.Conditions.Any(condition => condition.AttributeName == "name" && condition.Operator == ConditionOperator.Equal && Equals(condition.Values[0], "Salesperson")));
			var privilegeQuery = this.queries.Single(query => query.EntityName == "privilege");
			Assert.IsTrue(privilegeQuery.Criteria.Conditions.Any(condition => condition.AttributeName == "name" && condition.Operator == ConditionOperator.Equal && Equals(condition.Values[0], "prvWriteAccount")));
			CollectionAssert.AreEquivalent(new[] { "name", "canbebasic", "canbelocal", "canbedeep", "canbeglobal" }, privilegeQuery.ColumnSet.Columns.ToArray());
		}

		[TestMethod]
		public async Task TableAndActionShouldBuildAndValidateTechnicalName()
		{
			var command = Command();
			command.Name = null;
			command.Table = " Account ";
			command.Privilege = " Write ";
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("prvWriteAccount", this.queries.Single(query => query.EntityName == "privilege").Criteria.Conditions[0].Values[0]);
		}

		[TestMethod]
		public async Task RoleGuidShouldSelectSpecificRoleIncludingBusinessUnitCopies()
		{
			var command = Command();
			command.Role = this.roleId.ToString();
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var conditions = this.queries[0].Criteria.Conditions;
			Assert.HasCount(1, conditions);
			Assert.AreEqual("roleid", conditions[0].AttributeName);
			Assert.AreEqual(this.roleId, conditions[0].Values[0]);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow(" ")]
		[DataRow("null")]
		[DataRow(" NULL ")]
		[DataRow("0")]
		[DataRow(" 0 ")]
		public async Task EmptyOrNullLevelShouldRemoveOnlySelectedPrivilege(string? level)
		{
			foreach (var flag in new[] { "canbebasic", "canbelocal", "canbedeep", "canbeglobal" })
				this.privileges.Entities[0][flag] = false;
			var result = await this.executor.ExecuteAsync(Command(level), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(1, this.requests);
			var request = (RemovePrivilegeRoleRequest)this.requests[0];
			Assert.AreEqual(this.roleId, request.RoleId);
			Assert.AreEqual(this.privilegeId, request.PrivilegeId);
			Assert.AreEqual(true, result["Removed"]);
		}

		[TestMethod]
		[DataRow("Basic", "canbebasic")]
		[DataRow("Local", "canbelocal")]
		[DataRow("Deep", "canbedeep")]
		[DataRow("Global", "canbeglobal")]
		[DataRow("1", "canbebasic")]
		[DataRow("2", "canbelocal")]
		[DataRow("3", "canbedeep")]
		[DataRow("4", "canbeglobal")]
		public async Task UnsupportedLevelShouldFailWithoutWriting(string level, string flag)
		{
			this.privileges.Entities[0][flag] = false;
			var result = await this.executor.ExecuteAsync(Command(level), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not supported");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		[DataRow("Invalid")]
		[DataRow("-1")]
		[DataRow("5")]
		[DataRow("Basic, Global")]
		public async Task InvalidLevelShouldFailBeforeConnecting(string level)
		{
			var result = await this.executor.ExecuteAsync(Command(level), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			this.connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task UnknownPrivilegeShouldFailEvenForRemoval(bool remove)
		{
			this.privileges = new EntityCollection();
			var result = await this.executor.ExecuteAsync(Command(remove ? "null" : "Basic"), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "privilege table");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task UnknownConstructedPrivilegeShouldFailWithoutWriting()
		{
			var command = Command();
			command.Name = null;
			command.Table = "Account";
			command.Privilege = "NotAnAction";
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "prvNotAnActionAccount");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task PrivilegeLookupShouldMatchCaseInsensitivelyAndReturnCanonicalName()
		{
			var command = Command();
			command.Name = "prvwriteaccount";
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("prvWriteAccount", result["PrivilegeName"]);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task MissingOrAmbiguousRoleShouldFailWithoutWriting(bool ambiguous)
		{
			if (ambiguous)
				this.roles.Entities.Add(new Entity("role", Guid.NewGuid()) { ["name"] = "Salesperson" });
			else
				this.roles = new EntityCollection();
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, ambiguous ? "GUID" : "not found");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		[DataRow("Basic", false)]
		[DataRow("Basic", true)]
		[DataRow("null", false)]
		[DataRow("null", true)]
		[DataRow("0", false)]
		[DataRow("0", true)]
		public async Task ManagedRoleShouldFailBeforePrivilegeLookupOrWriting(string level, bool useGuid)
		{
			this.roles.Entities[0]["ismanaged"] = true;
			var command = Command(level);
			if (useGuid)
				command.Role = this.roleId.ToString();

			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "managed");
			StringAssert.Contains(result.ErrorMessage, "Salesperson");
			Assert.HasCount(1, this.queries);
			Assert.AreEqual("role", this.queries[0].EntityName);
			this.crm.Verify(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task DataverseFaultShouldReturnFailure()
		{
			this.crm.Setup(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Cannot edit role"));
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Cannot edit role");
		}

		[TestMethod]
		public async Task ConnectionFailureShouldReturnFailure()
		{
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ThrowsAsync(new InvalidOperationException("No active connection"));
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No active connection");
		}

		[TestMethod]
		public async Task CancellationShouldPropagateToQueriesAndRequest()
		{
			using var source = new CancellationTokenSource();
			var result = await this.executor.ExecuteAsync(Command(), source.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.crm.Verify(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), source.Token), Times.Exactly(2));
			this.crm.Verify(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), source.Token), Times.Once);
		}
	}
}