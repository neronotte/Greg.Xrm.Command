using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public abstract class RoleAssignmentCommandExecutorTestBase
	{
		protected abstract bool Revoke { get; }
		protected abstract RoleAssignmentCommand CreateCommand();
		protected abstract Task<CommandResult> RunAsync(RoleAssignmentService service, RoleAssignmentCommand command, CancellationToken cancellationToken);
		private readonly Guid userId = Guid.NewGuid();
		private readonly Guid teamId = Guid.NewGuid();
		private readonly Guid userBusinessUnitId = Guid.NewGuid();
		private readonly Guid teamBusinessUnitId = Guid.NewGuid();
		private readonly Guid selectedBusinessUnitId = Guid.NewGuid();
		private readonly Guid userRoleId = Guid.NewGuid();
		private readonly Guid teamRoleId = Guid.NewGuid();
		private readonly Guid selectedRoleId = Guid.NewGuid();
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly Mock<IOrganizationServiceRepository> connections = new();
		private readonly OutputToMemory output = new();
		private readonly Dictionary<string, List<Entity>> records = [];
		private readonly List<QueryExpression> queries = [];
		private readonly List<OrganizationRequest> requests = [];
		private readonly HashSet<(string Entity, Guid RecipientId, Guid RoleId)> assignments = [];
		private RoleAssignmentService service = null!;

		[TestInitialize]
		public void Initialize()
		{
			this.records["organization"] = [new Entity("organization", Guid.NewGuid())];
			this.records["systemuser"] = [new Entity("systemuser", this.userId)
			{
				["fullname"] = "John Doe", ["domainname"] = "user@contoso.com", ["internalemailaddress"] = "john@contoso.com",
				["businessunitid"] = new EntityReference("businessunit", this.userBusinessUnitId)
			}];
			this.records["team"] = [new Entity("team", this.teamId)
			{
				["name"] = "Sales", ["teamtype"] = new OptionSetValue(0),
				["businessunitid"] = new EntityReference("businessunit", this.teamBusinessUnitId)
			}];
			this.records["businessunit"] = [new Entity("businessunit", this.selectedBusinessUnitId) { ["name"] = "Europe" }];
			this.records["role"] =
			[
				Role(this.userRoleId, this.userBusinessUnitId),
				Role(this.teamRoleId, this.teamBusinessUnitId),
				Role(this.selectedRoleId, this.selectedBusinessUnitId)
			];
			if (this.Revoke)
			{
				this.assignments.Add(("systemuser", this.userId, this.userRoleId));
				this.assignments.Add(("team", this.teamId, this.teamRoleId));
				this.assignments.Add(("systemuser", this.userId, this.selectedRoleId));
				this.assignments.Add(("team", this.teamId, this.selectedRoleId));
			}
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(this.crm.Object);
			this.crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					var expression = (QueryExpression)query;
					this.queries.Add(expression);
					var matches = this.records[expression.EntityName].Where(entity => Matches(expression, entity));
					if (expression.LinkEntities.Count > 0)
					{
						var link = expression.LinkEntities.Single();
						var recipient = (Guid)link.LinkCriteria.Conditions.Single().Values[0];
						var name = link.LinkToEntityName == "systemuserroles" ? "systemuser" : "team";
						matches = matches.Where(entity => this.assignments.Contains((name, recipient, entity.Id)));
					}
					return Task.FromResult(new EntityCollection(matches.Take(expression.TopCount ?? int.MaxValue).ToList()));
				});
			this.crm.Setup(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Callback<OrganizationRequest, CancellationToken>((request, cancellationToken) =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					this.requests.Add(request);
				})
				.ReturnsAsync(new OrganizationResponse());
			this.service = new RoleAssignmentService(this.output, this.connections.Object, new SystemUser.Repository(),
				new Organization.Repository(), new BusinessUnit.Repository(), new Team.Repository(), new SecurityRole.Repository());
		}

		private static Entity Role(Guid roleId, Guid businessUnitId) => new("role", roleId)
		{
			["name"] = "Salesperson", ["businessunitid"] = new EntityReference("businessunit", businessUnitId), ["ismanaged"] = true
		};

		private static bool Matches(QueryExpression query, Entity entity)
		{
			var matches = query.Criteria.Conditions.Select(condition =>
			{
				entity.Attributes.TryGetValue(condition.AttributeName, out var attributeValue);
				object? value = condition.AttributeName == entity.LogicalName + "id" ? entity.Id : attributeValue;
				if (value is EntityReference reference) value = reference.Id;
				return Equals(value, condition.Values[0]);
			});
			return query.Criteria.FilterOperator == LogicalOperator.Or ? matches.Any(match => match) : matches.All(match => match);
		}

		private Task<CommandResult> ExecuteAsync(bool user = true, bool team = false, string? businessUnit = null,
			string? role = null, string? userIdentifier = null, string? teamIdentifier = null, CancellationToken cancellationToken = default)
		{
			var command = this.CreateCommand();
			command.Role = role ?? "Salesperson";
			command.User = user ? userIdentifier ?? "user@contoso.com" : null;
			command.Team = team ? teamIdentifier ?? "Sales" : null;
			command.BusinessUnit = businessUnit;
			return this.RunAsync(this.service, command, cancellationToken);
		}

		private void EnableCrossBusinessUnit(string value = "true") => this.records["organization"][0]["orgdborgsettings"] =
			$"<OrgSettings><EnableOwnershipAcrossBusinessUnits>{value}</EnableOwnershipAcrossBusinessUnits></OrgSettings>";

		[TestMethod]
		[DataRow(true, false)]
		[DataRow(false, true)]
		[DataRow(true, true)]
		public async Task RecipientsShouldUseTheirOwnBusinessUnitWhenDisabled(bool user, bool team)
		{
			var result = await this.ExecuteAsync(user, team, "Does not exist");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual((user ? 1 : 0) + (team ? 1 : 0), result["ChangedCount"]);
			Assert.AreEqual(0, result["SkippedCount"]);
			Assert.IsFalse(this.queries.Any(query => query.EntityName == "businessunit"));
			StringAssert.Contains(this.output.ToString(), "Ignoring --businessunit");
			if (user) AssertRequest(this.requests.Single(request => ((EntityReference)request["Target"]).LogicalName == "systemuser"), "systemuser", this.userId, this.userRoleId);
			if (team) AssertRequest(this.requests.Single(request => ((EntityReference)request["Target"]).LogicalName == "team"), "team", this.teamId, this.teamRoleId);
			Assert.IsTrue(this.queries.Where(query => query.EntityName == "role" && query.LinkEntities.Count == 0)
				.All(query => query.Criteria.Conditions.Any(condition => condition.AttributeName == "businessunitid") &&
					!query.Criteria.Conditions.Any(condition => condition.AttributeName == "parentroleid")));
		}

		private void AssertRequest(OrganizationRequest request, string entityName, Guid recipientId, Guid roleId)
		{
			Assert.AreEqual(this.Revoke ? "Disassociate" : "Associate", request.RequestName);
			var target = (EntityReference)request["Target"];
			Assert.AreEqual(entityName, target.LogicalName);
			Assert.AreEqual(recipientId, target.Id);
			var relationship = (Microsoft.Xrm.Sdk.Relationship)request["Relationship"];
			Assert.AreEqual(entityName == "systemuser" ? "systemuserroles_association" : "teamroles_association", relationship.SchemaName);
			var related = (EntityReferenceCollection)request["RelatedEntities"];
			Assert.HasCount(1, related);
			Assert.AreEqual("role", related[0].LogicalName);
			Assert.AreEqual(roleId, related[0].Id);
		}

		[TestMethod]
		[DataRow(true, false)]
		[DataRow(false, true)]
		[DataRow(true, true)]
		public async Task AlreadyDesiredStateShouldNotWrite(bool user, bool team)
		{
			this.assignments.Clear();
			if (!this.Revoke)
			{
				this.assignments.Add(("systemuser", this.userId, this.userRoleId));
				this.assignments.Add(("team", this.teamId, this.teamRoleId));
			}
			var result = await this.ExecuteAsync(user, team);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, result["ChangedCount"]);
			Assert.AreEqual((user ? 1 : 0) + (team ? 1 : 0), result["SkippedCount"]);
			Assert.IsEmpty(this.requests);
			StringAssert.Contains(this.output.ToString(), "Nothing to do");
			StringAssert.Contains(this.output.ToString(), this.Revoke ? "not assigned" : "already assigned");
		}

		[TestMethod]
		public async Task NoOpUserShouldNotPreventTeamChange()
		{
			if (this.Revoke) this.assignments.Remove(("systemuser", this.userId, this.userRoleId));
			else this.assignments.Add(("systemuser", this.userId, this.userRoleId));
			var result = await this.ExecuteAsync(true, true);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(false, result["UserChanged"]);
			Assert.AreEqual(true, result["TeamChanged"]);
			Assert.AreEqual(1, result["ChangedCount"]);
			Assert.AreEqual(1, result["SkippedCount"]);
			AssertRequest(this.requests.Single(), "team", this.teamId, this.teamRoleId);
		}

		[TestMethod]
		[DataRow("true")]
		[DataRow("True")]
		[DataRow("1")]
		public async Task EnabledSettingShouldRequireExplicitBusinessUnit(string value)
		{
			this.EnableCrossBusinessUnit(value);
			var result = await this.ExecuteAsync(true, true);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "--businessunit is required");
			Assert.IsEmpty(this.requests);
			Assert.HasCount(1, this.queries);
		}

		[TestMethod]
		[DataRow("false")]
		[DataRow("0")]
		public async Task DisabledSettingShouldNotRequireBusinessUnit(string value)
		{
			this.EnableCrossBusinessUnit(value);
			var result = await this.ExecuteAsync();
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			AssertRequest(this.requests.Single(), "systemuser", this.userId, this.userRoleId);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task EnabledSettingShouldUseSelectedBusinessUnitForBothRecipients(bool useGuid)
		{
			this.EnableCrossBusinessUnit();
			var identifier = useGuid ? this.selectedBusinessUnitId.ToString() : " Europe ";
			var result = await this.ExecuteAsync(true, true, identifier);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.selectedRoleId, result["UserRoleId"]);
			Assert.AreEqual(this.selectedRoleId, result["TeamRoleId"]);
			Assert.AreEqual(this.selectedBusinessUnitId, result["UserBusinessUnitId"]);
			Assert.AreEqual(this.selectedBusinessUnitId, result["TeamBusinessUnitId"]);
			AssertRequest(this.requests[0], "systemuser", this.userId, this.selectedRoleId);
			AssertRequest(this.requests[1], "team", this.teamId, this.selectedRoleId);
			var condition = this.queries.Single(query => query.EntityName == "businessunit").Criteria.Conditions.Single();
			Assert.AreEqual(useGuid ? "businessunitid" : "name", condition.AttributeName);
		}

		[TestMethod]
		public async Task GuidIdentifiersShouldResolveExactRecords()
		{
			this.EnableCrossBusinessUnit();
			var result = await this.ExecuteAsync(true, true, this.selectedBusinessUnitId.ToString(), this.selectedRoleId.ToString(), this.userId.ToString(), this.teamId.ToString());
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(2, this.requests);
			Assert.AreEqual("systemuserid", this.queries.Single(query => query.EntityName == "systemuser").Criteria.Conditions.Single().AttributeName);
			Assert.AreEqual("teamid", this.queries.Single(query => query.EntityName == "team").Criteria.Conditions.Single().AttributeName);
		}

		[TestMethod]
		public async Task AssignmentInAnotherBusinessUnitShouldNotAffectSelectedRole()
		{
			this.EnableCrossBusinessUnit();
			this.assignments.Clear();
			this.assignments.Add(("systemuser", this.userId, this.userRoleId));
			var result = await this.ExecuteAsync(businessUnit: "Europe");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.Revoke ? 0 : 1, result["ChangedCount"]);
			if (!this.Revoke) AssertRequest(this.requests.Single(), "systemuser", this.userId, this.selectedRoleId);
			else Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task SelectedRoleAlreadyInDesiredStateShouldNotWrite()
		{
			this.EnableCrossBusinessUnit();
			this.assignments.Clear();
			if (!this.Revoke)
			{
				this.assignments.Add(("systemuser", this.userId, this.selectedRoleId));
				this.assignments.Add(("team", this.teamId, this.selectedRoleId));
			}
			var result = await this.ExecuteAsync(true, true, "Europe");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, result["ChangedCount"]);
			Assert.AreEqual(2, result["SkippedCount"]);
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task PrimaryEmailShouldResolveUser()
		{
			var result = await this.ExecuteAsync(userIdentifier: " john@contoso.com ");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(this.userId, result["UserId"]);
		}

		[TestMethod]
		[DataRow("systemuser", false)]
		[DataRow("systemuser", true)]
		[DataRow("team", false)]
		[DataRow("team", true)]
		[DataRow("businessunit", false)]
		[DataRow("businessunit", true)]
		[DataRow("role", false)]
		[DataRow("role", true)]
		public async Task MissingOrAmbiguousMatchesShouldFailWithoutWriting(string entityName, bool ambiguous)
		{
			this.EnableCrossBusinessUnit();
			if (ambiguous)
			{
				var source = entityName == "role" ? this.records[entityName].Last() : this.records[entityName][0];
				var duplicate = new Entity(entityName, Guid.NewGuid());
				foreach (var attribute in source.Attributes) duplicate[attribute.Key] = attribute.Value;
				this.records[entityName].Add(duplicate);
			}
			else this.records[entityName].Clear();
			var result = await this.ExecuteAsync(true, true, "Europe");
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, ambiguous ? "GUID" : "not found");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task MissingTeamRoleShouldFailBeforeChangingUser()
		{
			this.records["role"].RemoveAll(entity => entity.Id == this.teamRoleId);
			var result = await this.ExecuteAsync(true, true);
			Assert.IsFalse(result.IsSuccess);
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task RoleGuidFromAnotherBusinessUnitShouldFail()
		{
			var result = await this.ExecuteAsync(role: this.teamRoleId.ToString());
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "role copy");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task AccessTeamShouldFailBeforeChangingUser()
		{
			this.records["team"][0]["teamtype"] = new OptionSetValue(1);
			var result = await this.ExecuteAsync(true, true);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "access team");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2)]
		[DataRow(3)]
		public async Task OwnerAndGroupTeamsShouldBeSupported(int teamType)
		{
			this.records["team"][0]["teamtype"] = new OptionSetValue(teamType);
			var result = await this.ExecuteAsync(false, true);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}

		[TestMethod]
		[DataRow("systemuser")]
		[DataRow("team")]
		public async Task MissingRecipientBusinessUnitShouldFailWhenDisabled(string entityName)
		{
			this.records[entityName][0].Attributes.Remove("businessunitid");
			var result = await this.ExecuteAsync(entityName == "systemuser", entityName == "team");
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No business unit");
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("<OrgSettings />")]
		public async Task AbsentSettingShouldDefaultToDisabled(string? xml)
		{
			this.records["organization"][0]["orgdborgsettings"] = xml;
			var result = await this.ExecuteAsync();
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}

		[TestMethod]
		[DataRow("<invalid")]
		[DataRow("<OrgSettings><EnableOwnershipAcrossBusinessUnits>invalid</EnableOwnershipAcrossBusinessUnits></OrgSettings>")]
		public async Task InvalidSettingShouldFailWithoutWriting(string xml)
		{
			this.records["organization"][0]["orgdborgsettings"] = xml;
			var result = await this.ExecuteAsync();
			Assert.IsFalse(result.IsSuccess);
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task MissingOrganizationShouldFailWithoutWriting()
		{
			this.records["organization"].Clear();
			var result = await this.ExecuteAsync();
			Assert.IsFalse(result.IsSuccess);
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task AssignmentChecksShouldOnlyQueryDirectRoles()
		{
			var result = await this.ExecuteAsync(true, true);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var checks = this.queries.Where(query => query.LinkEntities.Count > 0).ToList();
			Assert.HasCount(2, checks);
			Assert.AreEqual("systemuserroles", checks[0].LinkEntities.Single().LinkToEntityName);
			Assert.AreEqual(this.userId, checks[0].LinkEntities.Single().LinkCriteria.Conditions.Single().Values[0]);
			Assert.AreEqual(this.userRoleId, checks[0].Criteria.Conditions.Single().Values[0]);
			Assert.AreEqual("teamroles", checks[1].LinkEntities.Single().LinkToEntityName);
			Assert.AreEqual(this.teamId, checks[1].LinkEntities.Single().LinkCriteria.Conditions.Single().Values[0]);
			Assert.IsTrue(checks.All(query => query.LinkEntities.Single().LinkEntities.Count == 0));
		}

		[TestMethod]
		public async Task DataverseFaultShouldReturnFailure()
		{
			this.crm.Setup(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Simulated fault"));
			var result = await this.ExecuteAsync();
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Simulated fault");
		}

		[TestMethod]
		public async Task FirstWriteFailureShouldNotPreventSecondWrite()
		{
			this.crm.SetupSequence(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "User write failed"))
				.ReturnsAsync(new OrganizationResponse());

			var result = await this.ExecuteAsync(true, true);

			Assert.IsFalse(result.IsSuccess);
			this.crm.Verify(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
			this.crm.Verify(client => client.ExecuteAsync(It.Is<OrganizationRequest>(request =>
				((EntityReference)request["Target"]).LogicalName == "team"), It.IsAny<CancellationToken>()), Times.Once);
			Assert.AreEqual(1, result["ChangedCount"]);
			Assert.AreEqual(1, result["FailedCount"]);
			Assert.AreEqual(0, result["SkippedCount"]);
			Assert.AreEqual(false, result["UserChanged"]);
			Assert.AreEqual(true, result["TeamChanged"]);
			StringAssert.Contains(result.ErrorMessage, "User write failed");
		}

		[TestMethod]
		public async Task SecondWriteFailureShouldReportCompletedChanges()
		{
			this.crm.SetupSequence(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new OrganizationResponse())
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Team write failed"));
			var result = await this.ExecuteAsync(true, true);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Team write failed");
			StringAssert.Contains(result.ErrorMessage, "Completed changes: 1");
			Assert.AreEqual(1, result["ChangedCount"]);
			Assert.AreEqual(1, result["FailedCount"]);
			Assert.AreEqual(0, result["SkippedCount"]);
			Assert.AreEqual(true, result["UserChanged"]);
			Assert.AreEqual(false, result["TeamChanged"]);
		}

		[TestMethod]
		public async Task BothWriteFailuresShouldBeReportedWithoutSkippingSecondAttempt()
		{
			this.crm.SetupSequence(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "User write failed"))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Team write failed"));
			var result = await this.ExecuteAsync(true, true);
			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, result["ChangedCount"]);
			Assert.AreEqual(2, result["FailedCount"]);
			Assert.AreEqual(0, result["SkippedCount"]);
			StringAssert.Contains(result.ErrorMessage, "User write failed");
			StringAssert.Contains(result.ErrorMessage, "Team write failed");
			Assert.AreEqual("User write failed", result["UserError"]);
			Assert.AreEqual("Team write failed", result["TeamError"]);
			this.crm.Verify(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
		}

		[TestMethod]
		public async Task FailedWriteAndNoOpShouldHaveSeparateCounts()
		{
			if (this.Revoke) this.assignments.Remove(("team", this.teamId, this.teamRoleId));
			else this.assignments.Add(("team", this.teamId, this.teamRoleId));
			this.crm.Setup(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "User write failed"));
			var result = await this.ExecuteAsync(true, true);
			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(0, result["ChangedCount"]);
			Assert.AreEqual(1, result["FailedCount"]);
			Assert.AreEqual(1, result["SkippedCount"]);
			Assert.AreEqual(false, result["TeamChanged"]);
			StringAssert.Contains(this.output.ToString(), "Nothing to do");
			this.crm.Verify(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task CancellationDuringFirstWriteShouldPreventSecondWrite()
		{
			using var cancellation = new CancellationTokenSource();
			this.crm.Setup(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), cancellation.Token))
				.Returns(() =>
				{
					cancellation.Cancel();
					return Task.FromCanceled<OrganizationResponse>(cancellation.Token);
				});
			await Assert.ThrowsAsync<OperationCanceledException>(() => this.ExecuteAsync(true, true, cancellationToken: cancellation.Token));
			this.crm.Verify(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), cancellation.Token), Times.Once);
		}

		[TestMethod]
		public async Task CancellationDuringReadShouldPropagate()
		{
			using var cancellation = new CancellationTokenSource();
			this.crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token))
				.Returns((QueryBase query, CancellationToken cancellationToken) =>
				{
					cancellation.Cancel();
					return Task.FromCanceled<EntityCollection>(cancellationToken);
				});
			await Assert.ThrowsAsync<OperationCanceledException>(() => this.ExecuteAsync(cancellationToken: cancellation.Token));
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task CancellationShouldPropagateWithoutConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => this.ExecuteAsync(cancellationToken: cancellation.Token));
			this.connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task CancellationTokenShouldReachReadsAndWrites()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await this.ExecuteAsync(cancellationToken: cancellation.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.crm.Verify(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token), Times.Exactly(4));
			this.crm.Verify(client => client.ExecuteAsync(It.IsAny<OrganizationRequest>(), cancellation.Token), Times.Once);
		}

		[TestMethod]
		public void ExecutorsShouldResolveUsingCoreModule()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new IoCModule());
			builder.RegisterInstance(this.connections.Object).As<IOrganizationServiceRepository>();
			builder.RegisterInstance(this.output).As<IOutput>();
			builder.RegisterType<AssignCommandExecutor>();
			builder.RegisterType<RevokeCommandExecutor>();
			using var container = builder.Build();
			Assert.IsNotNull(container.Resolve<AssignCommandExecutor>());
			Assert.IsNotNull(container.Resolve<RevokeCommandExecutor>());
		}
	}
}