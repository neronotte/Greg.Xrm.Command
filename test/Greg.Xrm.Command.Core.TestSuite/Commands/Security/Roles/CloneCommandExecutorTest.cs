using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class CloneCommandExecutorTest
	{
		private readonly Guid sourceId = Guid.NewGuid();
		private readonly Guid sourceBusinessUnitId = Guid.NewGuid();
		private readonly Guid destinationBusinessUnitId = Guid.NewGuid();
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly Mock<IOrganizationServiceRepository> connections = new();
		private readonly OutputToMemory output = new();
		private readonly List<QueryExpression> queries = [];
		private readonly List<OrganizationRequest> requests = [];
		private EntityCollection source = new();
		private EntityCollection organizations = new();
		private EntityCollection businessUnits = new();
		private readonly List<EntityCollection> namePages = [];
		private readonly List<int> namePageNumbers = [];
		private RolePrivilege[] privileges = [];
		private CloneCommandExecutor executor = null!;

		[TestInitialize]
		public void Initialize()
		{
			this.source = new EntityCollection([new Entity("role", this.sourceId)
			{
				["name"] = "Salesperson", ["description"] = "Original description", ["isinherited"] = new OptionSetValue(1),
				["businessunitid"] = new EntityReference("businessunit", this.sourceBusinessUnitId), ["ismanaged"] = true
			}]);
			this.organizations = new EntityCollection([new Entity("organization", Guid.NewGuid())]);
			this.businessUnits = new EntityCollection([new Entity("businessunit", this.destinationBusinessUnitId) { ["name"] = "Europe" }]);
			this.namePages.Add(new EntityCollection([new Entity("role", this.sourceId) { ["name"] = "Salesperson" }]));
			this.privileges = Enum.GetValues<PrivilegeDepth>().Select(depth => new RolePrivilege { PrivilegeId = Guid.NewGuid(), Depth = depth }).ToArray();
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(this.crm.Object);
			this.crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					var expression = (QueryExpression)query;
					this.queries.Add(expression);
					if (expression.EntityName == "organization") return Task.FromResult(this.organizations);
					if (expression.EntityName == "businessunit") return Task.FromResult(this.businessUnits);
					if (expression.Criteria.Conditions.Count > 0) return Task.FromResult(this.source);
					this.namePageNumbers.Add(expression.PageInfo.PageNumber);
					return Task.FromResult(this.namePages[expression.PageInfo.PageNumber - 1]);
				});
			this.crm.Setup(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Returns((OrganizationRequest request, CancellationToken cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					this.requests.Add(request);
					if (request is RetrieveRolePrivilegesRoleRequest)
					{
						var response = new RetrieveRolePrivilegesRoleResponse();
						response.Results["RolePrivileges"] = this.privileges;
						return Task.FromResult<OrganizationResponse>(response);
					}
					return Task.FromResult(new OrganizationResponse());
				});
			this.executor = new CloneCommandExecutor(this.output, this.connections.Object, new SecurityRole.Repository(),
				new BusinessUnit.Repository());
		}

		private static CloneCommand Command() => new() { Role = "Salesperson" };
		private Entity CreatedRole => ((CreateRequest)this.requests.OfType<ExecuteTransactionRequest>().Single().Requests[0]).Target;

		private void ExistingNames(params string[] names)
		{
			this.namePages.Clear();
			this.namePages.Add(new EntityCollection(names.Select(name => new Entity("role", Guid.NewGuid()) { ["name"] = name }).ToList()));
		}

		[TestMethod]
		public async Task CloneShouldPreserveSettingsAndAllPrivilegeDepthsAtomically()
		{
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.sourceId, result["SourceRoleId"]);
			Assert.AreEqual("Salesperson - Copy", result["RoleName"]);
			Assert.AreEqual(this.sourceBusinessUnitId, result["BusinessUnitId"]);
			Assert.AreEqual("DirectUserAndTeam", result["Inheritance"]);
			Assert.AreEqual(this.privileges.Length, result["PrivilegeCount"]);
			Assert.HasCount(2, this.requests);
			Assert.AreEqual(this.sourceId, ((RetrieveRolePrivilegesRoleRequest)this.requests[0]).RoleId);
			var transaction = (ExecuteTransactionRequest)this.requests[1];
			Assert.HasCount(2, transaction.Requests);
			Assert.IsFalse(transaction.ReturnResponses);
			var created = this.CreatedRole;
			Assert.AreNotEqual(this.sourceId, created.Id);
			Assert.AreEqual(created.Id, result["RoleId"]);
			Assert.AreEqual("role", created.LogicalName);
			Assert.AreEqual("Original description", created["description"]);
			Assert.AreEqual(1, ((OptionSetValue)created["isinherited"]).Value);
			Assert.AreEqual(this.sourceBusinessUnitId, ((EntityReference)created["businessunitid"]).Id);
			CollectionAssert.AreEquivalent(new[] { "name", "description", "isinherited", "businessunitid" }, created.Attributes.Keys.ToArray());
			var replace = (ReplacePrivilegesRoleRequest)transaction.Requests[1];
			Assert.AreEqual(created.Id, replace.RoleId);
			Assert.AreEqual(this.privileges.Length, replace.Privileges.Length);
			for (var index = 0; index < this.privileges.Length; index++)
			{
				Assert.AreEqual(this.privileges[index].PrivilegeId, replace.Privileges[index].PrivilegeId);
				Assert.AreEqual(this.privileges[index].Depth, replace.Privileges[index].Depth);
			}
			var query = this.queries[0];
			Assert.IsTrue(query.Criteria.Conditions.Any(condition => condition.AttributeName == "parentroleid" && condition.Operator == ConditionOperator.Null));
			Assert.IsFalse(this.queries.Any(expression => expression.EntityName == "organization"));
		}

		[TestMethod]
		[DataRow(MemberPrivilegeInheritance.TeamOnly)]
		[DataRow(MemberPrivilegeInheritance.DirectUserAndTeam)]
		public async Task OverridesShouldBeApplied(MemberPrivilegeInheritance inheritance)
		{
			var command = Command();
			command.Name = " Regional Sales ";
			command.Description = "Custom description";
			command.BusinessUnit = " Europe ";
			command.Inheritance = inheritance;
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("Regional Sales", this.CreatedRole["name"]);
			Assert.AreEqual("Custom description", this.CreatedRole["description"]);
			Assert.AreEqual((int)inheritance, ((OptionSetValue)this.CreatedRole["isinherited"]).Value);
			Assert.AreEqual(this.destinationBusinessUnitId, ((EntityReference)this.CreatedRole["businessunitid"]).Id);
			Assert.AreEqual("Europe", this.queries.Single(query => query.EntityName == "businessunit").Criteria.Conditions.Single().Values[0]);
		}

		[TestMethod]
		public async Task GuidSourceShouldSelectExactBusinessUnitCopy()
		{
			var command = Command();
			command.Role = this.sourceId.ToString();
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var condition = this.queries[0].Criteria.Conditions.Single();
			Assert.AreEqual("roleid", condition.AttributeName);
			Assert.AreEqual(this.sourceId, condition.Values[0]);
		}

		[TestMethod]
		public async Task GuidBusinessUnitShouldSelectExactDestination()
		{
			var command = Command();
			command.BusinessUnit = this.destinationBusinessUnitId.ToString();
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var condition = this.queries.Single(query => query.EntityName == "businessunit").Criteria.Conditions.Single();
			Assert.AreEqual("businessunitid", condition.AttributeName);
			Assert.AreEqual(this.destinationBusinessUnitId, condition.Values[0]);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("<OrgSettings><EnableOwnershipAcrossBusinessUnits>false</EnableOwnershipAcrossBusinessUnits></OrgSettings>")]
		[DataRow("<OrgSettings><EnableOwnershipAcrossBusinessUnits>true</EnableOwnershipAcrossBusinessUnits></OrgSettings>")]
		[DataRow("<invalid")]
		public async Task BusinessUnitShouldAlwaysBeHonoredWithoutReadingOrganizationSettings(string? xml)
		{
			this.organizations.Entities[0]["orgdborgsettings"] = xml;
			var command = Command();
			command.BusinessUnit = "Europe";
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.destinationBusinessUnitId, ((EntityReference)this.CreatedRole["businessunitid"]).Id);
			Assert.IsTrue(this.queries.Any(query => query.EntityName == "businessunit"));
			Assert.IsFalse(this.queries.Any(query => query.EntityName == "organization"));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		public async Task OmittedInheritanceShouldPreserveSource(int inheritance)
		{
			this.source.Entities[0]["isinherited"] = new OptionSetValue(inheritance);
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(inheritance, ((OptionSetValue)this.CreatedRole["isinherited"]).Value);
		}

		[TestMethod]
		public async Task MissingInheritanceShouldUseDataverseDefault()
		{
			this.source.Entities[0].Attributes.Remove("isinherited");
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(1, ((OptionSetValue)this.CreatedRole["isinherited"]).Value);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		public async Task DescriptionShouldBePreservedOrExplicitlyCleared(string? description)
		{
			var command = Command();
			command.Description = description;
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(description ?? "Original description", this.CreatedRole["description"]);
		}

		[TestMethod]
		[DataRow(new string[] { }, "Salesperson - Copy")]
		[DataRow(new[] { "Salesperson - Copy" }, "Salesperson - Copy 2")]
		[DataRow(new[] { "SALESPERSON - COPY", "Salesperson - Copy 2" }, "Salesperson - Copy 3")]
		[DataRow(new[] { "Salesperson - Copy 8" }, "Salesperson - Copy 9")]
		[DataRow(new[] { "Other - Copy 20", "Salesperson - Copy notes", "Salesperson - Copy 02" }, "Salesperson - Copy")]
		public async Task AutomaticNameShouldContinueAboveExistingCopies(string[] existing, string expected)
		{
			this.ExistingNames(existing);
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(expected, result["RoleName"]);
		}

		[TestMethod]
		public async Task AutomaticNamesShouldRespectLengthLimitAndKeepNumericSuffix()
		{
			var name = new string('x', 100);
			this.source.Entities[0]["name"] = name;
			this.ExistingNames(name[..93] + " - Copy", name[..91] + " - Copy 2");
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(name[..91] + " - Copy 3", result["RoleName"]);
			Assert.AreEqual(100, ((string)result["RoleName"]).Length);
		}

		[TestMethod]
		public async Task NameChecksShouldIncludeAllBusinessUnitsAndPages()
		{
			this.namePages[0].MoreRecords = true;
			this.namePages[0].PagingCookie = "cookie";
			this.namePages.Add(new EntityCollection([new Entity("role", Guid.NewGuid()) { ["name"] = "Salesperson - Copy 4" }]));
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("Salesperson - Copy 5", result["RoleName"]);
			CollectionAssert.AreEqual(new[] { 1, 2 }, this.namePageNumbers);
			Assert.IsTrue(this.queries.Where(query => query.Criteria.Conditions.Count == 0).All(query => query.Criteria.Filters.Count == 0 && !query.TopCount.HasValue));
		}

		[TestMethod]
		public async Task DuplicateExplicitNameShouldFailWithoutWriting()
		{
			this.ExistingNames("Regional Sales");
			var command = Command();
			command.Name = "regional sales";
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "already exists");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task MissingOrAmbiguousSourceShouldFailWithoutWriting(bool ambiguous)
		{
			if (ambiguous) this.source.Entities.Add(new Entity("role", Guid.NewGuid()) { ["name"] = "Salesperson" });
			else this.source.Entities.Clear();
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, ambiguous ? "GUID" : "not found");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task MissingOrAmbiguousBusinessUnitShouldFailWithoutWriting(bool ambiguous)
		{
			if (ambiguous) this.businessUnits.Entities.Add(new Entity("businessunit", Guid.NewGuid()) { ["name"] = "Europe" });
			else this.businessUnits.Entities.Clear();
			var command = Command();
			command.BusinessUnit = "Europe";
			var result = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, ambiguous ? "GUID" : "not found");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task MissingSourceBusinessUnitShouldFailWithoutWriting()
		{
			this.source.Entities[0].Attributes.Remove("businessunitid");
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "no business unit");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task UnsupportedSourceInheritanceShouldFailUnlessOverridden()
		{
			this.source.Entities[0]["isinherited"] = new OptionSetValue(2);
			var command = Command();
			var invalid = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(invalid.IsSuccess);
			Assert.IsEmpty(this.requests);
			command.Inheritance = MemberPrivilegeInheritance.TeamOnly;
			var valid = await this.executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsTrue(valid.IsSuccess, valid.ErrorMessage);
		}

		[TestMethod]
		public async Task EmptyPrivilegeSetShouldStillReplaceDefaultPrivileges()
		{
			this.privileges = [];
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, result["PrivilegeCount"]);
			Assert.IsEmpty(((ReplacePrivilegesRoleRequest)this.requests.OfType<ExecuteTransactionRequest>().Single().Requests[1]).Privileges);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task PrivilegeReadOrTransactionFaultShouldReturnFailure(bool transaction)
		{
			this.crm.Setup(client => client.ExecuteAsync(It.Is<OrganizationRequest>(request =>
				transaction ? request is ExecuteTransactionRequest : request is RetrieveRolePrivilegesRoleRequest), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Clone failed"));
			var result = await this.executor.ExecuteAsync(Command(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Clone failed");
			if (!transaction) this.crm.Verify(client => client.ExecuteAsync(It.Is<OrganizationRequest>(request => request is ExecuteTransactionRequest), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task CancellationShouldPropagateBeforeConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => this.executor.ExecuteAsync(Command(), cancellation.Token));
			this.connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task CancellationTokenShouldReachReadsAndTransaction()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await this.executor.ExecuteAsync(Command(), cancellation.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.crm.Verify(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token), Times.Exactly(2));
			this.crm.Verify(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), cancellation.Token), Times.Exactly(2));
		}

		[TestMethod]
		public void ExecutorShouldResolveUsingCoreModule()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new IoCModule());
			builder.RegisterInstance(this.connections.Object).As<IOrganizationServiceRepository>();
			builder.RegisterInstance(this.output).As<IOutput>();
			builder.RegisterType<CloneCommandExecutor>().As<ICommandExecutor<CloneCommand>>();
			using var container = builder.Build();
			Assert.IsInstanceOfType<CloneCommandExecutor>(container.Resolve<ICommandExecutor<CloneCommand>>());
		}
	}
}