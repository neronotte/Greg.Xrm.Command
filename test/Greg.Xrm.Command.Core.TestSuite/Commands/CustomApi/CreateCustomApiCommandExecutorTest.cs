using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CreateCustomApiCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CreateCustomApiCommandExecutor executor;
		private readonly Mock<ISolutionRepository> solutionRepositoryMock;

		private const string DefaultSolution = "TestSolution";

		public CreateCustomApiCommandExecutorTest()
		{
			this.solutionRepositoryMock = new Mock<ISolutionRepository>();

			// Default solution
			this.OrganizationServiceRepositoryMock
				.Setup(x => x.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync(DefaultSolution);

			// Solution lookup returns an unmanaged solution by default
			// Solution has a protected ctor; use reflection to create an instance for tests.
			var solEntity = new Entity("solution") { Id = Guid.NewGuid() };
			solEntity["ismanaged"] = false;
			solEntity["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", "nn");
			var sol = (Greg.Xrm.Command.Model.Solution)Activator.CreateInstance(
				typeof(Greg.Xrm.Command.Model.Solution),
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
				null, new object[] { solEntity }, null)!;
			this.solutionRepositoryMock
				.Setup(x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync(sol);

			// AddSolutionComponentRequest always succeeds
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>()))
				.ReturnsAsync(new AddSolutionComponentResponse());
			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new AddSolutionComponentResponse());

			this.executor = new CreateCustomApiCommandExecutor(
				this.Output,
				this.OrganizationServiceRepositoryMock.Object,
				this.solutionRepositoryMock.Object);
		}

		private void SetupNoExistingApi()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
		}

		private void SetupCreateReturnsNewId(string logicalName)
		{
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == logicalName)))
				.ReturnsAsync(Guid.NewGuid());
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == logicalName), It.IsAny<CancellationToken>()))
				.ReturnsAsync(Guid.NewGuid());
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenCreatingMinimalApi()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldAddApiToSolution_WhenCreated()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(DefaultSolution, result["Solution"]);

			this.OrganizationServiceMock.Verify(
				x => x.ExecuteAsync(It.Is<AddSolutionComponentRequest>(r =>
					r.SolutionUniqueName == DefaultSolution &&
						r.ComponentType == (int)ComponentType.CustomAPI), It.IsAny<CancellationToken>()),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldUseSolutionFromCommand_WhenProvided()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum", SolutionName = "MySolution" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual("MySolution", result["Solution"]);

			this.solutionRepositoryMock.Verify(
				x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), "MySolution"),
				Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenSolutionNotFound()
		{
			this.solutionRepositoryMock
				.Setup(x => x.GetByUniqueNameAsync(It.IsAny<IOrganizationServiceAsync2>(), It.IsAny<string>()))
				.ReturnsAsync((Greg.Xrm.Command.Model.Solution?)null);

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "not found");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldCreateParamsAndResponses_WhenProvided()
		{
			SetupNoExistingApi();
			SetupCreateReturnsNewId("customapi");

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapirequestparameter"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapiresponseproperty"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
			SetupCreateReturnsNewId("customapirequestparameter");
			SetupCreateReturnsNewId("customapiresponseproperty");

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand
				{
					DisplayName = "Greg Sum",
					UniqueName  = "nn_GregSum",
					Params      = "Addend1:Integer,Addend2:Integer",
					Responses   = "Result:Integer"
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapirequestparameter")),
				Times.Exactly(2));
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapiresponseproperty")),
				Times.Once);
			// All 4 components added to solution: 1 api + 2 params + 1 response
			this.OrganizationServiceMock.Verify(
					x => x.ExecuteAsync(It.IsAny<AddSolutionComponentRequest>(), It.IsAny<CancellationToken>()),
				Times.Exactly(4));
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldSucceed_WhenApiAlreadyExists()
		{
			// Idempotency: existing API found -> skip creation, return success
			var existing = new Entity("customapi") { Id = Guid.NewGuid() };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existing }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "customapi"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { existing }));

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(
				x => x.CreateAsync(It.IsAny<Entity>()),
				Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldInferUniqueName_WhenNotProvided()
		{
			SetupNoExistingApi();

			string? capturedUniqueName = null;
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")))
				.Callback<Entity>(e => capturedUniqueName = e.GetAttributeValue<string>("uniquename"))
				.ReturnsAsync(Guid.NewGuid());

			await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum" },
				CancellationToken.None);

			// publisher prefix is "nn" (set in constructor); "Greg Sum" -> "nn_GregSum"
			Assert.AreEqual("nn_GregSum", capturedUniqueName);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenUniqueNamePrefixMismatchesSolution()
		{
			SetupNoExistingApi();

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "wrong_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "wrong");
			StringAssert.Contains(result.ErrorMessage, "nn");
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldFail_WhenNoSolutionNameAndNoDefault()
		{
			this.OrganizationServiceRepositoryMock
				.Setup(x => x.GetCurrentDefaultSolutionAsync())
				.ReturnsAsync((string?)null);

			var result = await executor.ExecuteAsync(
				new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum" },
				CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No solution name");
		}

			[TestMethod]
				public async Task ExecuteAsync_ShouldFail_WhenDataverseThrows()
				{
					SetupNoExistingApi();

					this.OrganizationServiceMock
						.Setup(x => x.CreateAsync(It.IsAny<Entity>()))
						.ThrowsAsync(new FaultException<OrganizationServiceFault>(
							new OrganizationServiceFault(), "Simulated fault"));

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand { DisplayName = "Greg Sum", UniqueName = "nn_GregSum" },
						CancellationToken.None);

					Assert.IsFalse(result.IsSuccess);
				}

				#region ExecutePrivilegeName validation tests

				[TestMethod]
				public async Task ExecuteAsync_ShouldSucceed_WhenExecutePrivilegeNameIsNull()
				{
					SetupNoExistingApi();
					SetupCreateReturnsNewId("customapi");

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand
						{
							DisplayName = "Greg Sum",
							UniqueName = "nn_GregSum",
							ExecutePrivilegeName = null
						},
						CancellationToken.None);

					Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				}

				[TestMethod]
				public async Task ExecuteAsync_ShouldSucceed_WhenExactPrivilegeMatchFound()
				{
					SetupNoExistingApi();
					SetupCreateReturnsNewId("customapi");

					// Exact match query returns one privilege
					var exactMatchPrivilege = new Entity("privilege") { Id = Guid.NewGuid() };
					exactMatchPrivilege["name"] = "prvReadAccount";

					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Equal))))
						.ReturnsAsync(new EntityCollection(new List<Entity> { exactMatchPrivilege }));

					string? capturedPrivilegeName = null;
					this.OrganizationServiceMock
						.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")))
						.Callback<Entity>(e => capturedPrivilegeName = e.GetAttributeValue<string>("executeprivilegename"))
						.ReturnsAsync(Guid.NewGuid());

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand
						{
							DisplayName = "Greg Sum",
							UniqueName = "nn_GregSum",
							ExecutePrivilegeName = "prvReadAccount"
						},
						CancellationToken.None);

					Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
					Assert.AreEqual("prvReadAccount", capturedPrivilegeName);
				}

				[TestMethod]
				public async Task ExecuteAsync_ShouldSucceed_WhenOneFuzzyPrivilegeMatchFound()
				{
					SetupNoExistingApi();
					SetupCreateReturnsNewId("customapi");

					// Exact match returns nothing
					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Equal))))
						.ReturnsAsync(new EntityCollection());

					// Fuzzy match returns one privilege
					var fuzzyMatchPrivilege = new Entity("privilege") { Id = Guid.NewGuid() };
					fuzzyMatchPrivilege["name"] = "prvReadAccount";

					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Like))))
						.ReturnsAsync(new EntityCollection(new List<Entity> { fuzzyMatchPrivilege }));

					string? capturedPrivilegeName = null;
					this.OrganizationServiceMock
						.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")))
						.Callback<Entity>(e => capturedPrivilegeName = e.GetAttributeValue<string>("executeprivilegename"))
						.ReturnsAsync(Guid.NewGuid());

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand
						{
							DisplayName = "Greg Sum",
							UniqueName = "nn_GregSum",
							ExecutePrivilegeName = "ReadAccount"
						},
						CancellationToken.None);

					Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
					Assert.AreEqual("prvReadAccount", capturedPrivilegeName);
				}

				[TestMethod]
				public async Task ExecuteAsync_ShouldFail_WhenNoPrivilegeMatchFound()
				{
					SetupNoExistingApi();

					// Exact match returns nothing
					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Equal))))
						.ReturnsAsync(new EntityCollection());

					// Fuzzy match returns nothing
					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Like))))
						.ReturnsAsync(new EntityCollection());

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand
						{
							DisplayName = "Greg Sum",
							UniqueName = "nn_GregSum",
							ExecutePrivilegeName = "NonExistentPrivilege"
						},
						CancellationToken.None);

					Assert.IsFalse(result.IsSuccess);
					StringAssert.Contains(result.ErrorMessage, "Invalid execute privilege name");
				}

				[TestMethod]
				public async Task ExecuteAsync_ShouldFail_WhenMultipleFuzzyPrivilegeMatchesFound()
				{
					SetupNoExistingApi();

					// Exact match returns nothing
					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Equal))))
						.ReturnsAsync(new EntityCollection());

					// Fuzzy match returns multiple privileges (ambiguity)
					var privilege1 = new Entity("privilege") { Id = Guid.NewGuid() };
					privilege1["name"] = "prvReadAccount";
					var privilege2 = new Entity("privilege") { Id = Guid.NewGuid() };
					privilege2["name"] = "prvWriteAccount";

					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Like))))
						.ReturnsAsync(new EntityCollection(new List<Entity> { privilege1, privilege2 }));

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand
						{
							DisplayName = "Greg Sum",
							UniqueName = "nn_GregSum",
							ExecutePrivilegeName = "Account"
						},
						CancellationToken.None);

					Assert.IsFalse(result.IsSuccess);
					StringAssert.Contains(result.ErrorMessage, "Invalid execute privilege name");
				}

		[TestMethod]
		public async Task ExecuteAsync_ShouldEscapeWildcardCharactersInPrivilegeSearch()
		{
			SetupNoExistingApi();

			// Exact match returns nothing
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
					q.EntityName == "privilege" &&
					q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Equal))))
				.ReturnsAsync(new EntityCollection());

			// Capture the LIKE query to verify wildcard escaping
			string? capturedLikeValue = null;
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
					q.EntityName == "privilege" &&
					q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Like))))
				.Returns((QueryBase qb) =>
				{
					var q = (QueryExpression)qb;
					var likeCondition = q.Criteria.Conditions.First(c => c.Operator == ConditionOperator.Like);
					capturedLikeValue = likeCondition.Values[0]?.ToString();
					return Task.FromResult(new EntityCollection());
				});

			await executor.ExecuteAsync(
				new CreateCustomApiCommand
				{
					DisplayName = "Greg Sum",
					UniqueName = "nn_GregSum",
					ExecutePrivilegeName = "prv_Read%Account[1]"
				},
				CancellationToken.None);

			// Verify that _, %, and [ are escaped in the LIKE query
			Assert.IsNotNull(capturedLikeValue);
			StringAssert.Contains(capturedLikeValue, "[_]");  // _ escaped
			StringAssert.Contains(capturedLikeValue, "[%]");  // % escaped
			StringAssert.Contains(capturedLikeValue, "[[]");  // [ escaped
		}

				[TestMethod]
				public async Task ExecuteAsync_ShouldPreferExactMatchOverFuzzyMatch()
				{
					SetupNoExistingApi();
					SetupCreateReturnsNewId("customapi");

					// Exact match returns the exact privilege
					var exactMatchPrivilege = new Entity("privilege") { Id = Guid.NewGuid() };
					exactMatchPrivilege["name"] = "prvRead";

					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Equal))))
						.ReturnsAsync(new EntityCollection(new List<Entity> { exactMatchPrivilege }));

					// Fuzzy match would return multiple (but shouldn't be called)
					var fuzzyMatch1 = new Entity("privilege") { Id = Guid.NewGuid() };
					fuzzyMatch1["name"] = "prvRead";
					var fuzzyMatch2 = new Entity("privilege") { Id = Guid.NewGuid() };
					fuzzyMatch2["name"] = "prvReadAccount";

					this.OrganizationServiceMock
						.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Like))))
						.ReturnsAsync(new EntityCollection(new List<Entity> { fuzzyMatch1, fuzzyMatch2 }));

					string? capturedPrivilegeName = null;
					this.OrganizationServiceMock
						.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "customapi")))
						.Callback<Entity>(e => capturedPrivilegeName = e.GetAttributeValue<string>("executeprivilegename"))
						.ReturnsAsync(Guid.NewGuid());

					var result = await executor.ExecuteAsync(
						new CreateCustomApiCommand
						{
							DisplayName = "Greg Sum",
							UniqueName = "nn_GregSum",
							ExecutePrivilegeName = "prvRead"
						},
						CancellationToken.None);

					Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
					Assert.AreEqual("prvRead", capturedPrivilegeName);

					// Verify exact match was used, not fuzzy
					this.OrganizationServiceMock.Verify(
						x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q =>
							q.EntityName == "privilege" &&
							q.Criteria.Conditions.Any(c => c.Operator == ConditionOperator.Like))),
						Times.Never);
				}

				#endregion
			}
		}
