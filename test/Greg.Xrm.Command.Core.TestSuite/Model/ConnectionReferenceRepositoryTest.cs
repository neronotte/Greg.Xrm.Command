using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	[TestClass]
	public class ConnectionReferenceRepositoryTest
	{
		private readonly ConnectionReference.Repository repository = new();
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();

		private QueryExpression? capturedQuery;

		public ConnectionReferenceRepositoryTest()
		{
			this.crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.Callback<QueryBase>(q => this.capturedQuery = q as QueryExpression)
				.ReturnsAsync(new EntityCollection());
		}

		private static Entity Reference(string? logicalName)
		{
			var entity = new Entity("connectionreference", Guid.NewGuid());
			entity["connectionreferencelogicalname"] = logicalName;
			return entity;
		}


		[TestMethod]
		public async Task GetExistingLogicalNamesAsync_ShouldQueryTheConnectionReferenceTable_WithTheGivenNames()
		{
			await repository.GetExistingLogicalNamesAsync(crmMock.Object, ["new_a_123", "new_b_456"]);

			Assert.IsNotNull(this.capturedQuery);
			Assert.AreEqual("connectionreference", this.capturedQuery.EntityName);
			var condition = this.capturedQuery.Criteria.Conditions.Single();
			Assert.AreEqual("connectionreferencelogicalname", condition.AttributeName);
			Assert.AreEqual(ConditionOperator.In, condition.Operator);
			CollectionAssert.AreEquivalent(new[] { "new_a_123", "new_b_456" }, condition.Values.Cast<string>().ToArray(), "Dropping the name filter would make every missing reference look existing.");
		}

		[TestMethod]
		public async Task GetExistingLogicalNamesAsync_ShouldNotCallTheServer_ForAnEmptyInput()
		{
			var result = await repository.GetExistingLogicalNamesAsync(crmMock.Object, []);

			Assert.AreEqual(0, result.Count);
			this.crmMock.Verify(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()), Times.Never);
		}

		[TestMethod]
		public async Task GetExistingLogicalNamesAsync_ShouldReturnTheNames_IgnoringCaseAndBlanks()
		{
			this.crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection([Reference("new_a_123"), Reference(null), Reference("  ")]));

			var result = await repository.GetExistingLogicalNamesAsync(crmMock.Object, ["NEW_A_123"]);

			Assert.AreEqual(1, result.Count);
			Assert.IsTrue(result.Contains("NEW_A_123", StringComparer.OrdinalIgnoreCase));
		}
	}


	[TestClass]
	public class WorkflowRepositoryQueryTest
	{
		private readonly Workflow.Repository repository = new();
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();

		private QueryExpression? capturedQuery;

		public WorkflowRepositoryQueryTest()
		{
			this.crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.Callback<QueryBase>(q => this.capturedQuery = q as QueryExpression)
				.ReturnsAsync(new EntityCollection());
		}

		[TestMethod]
		public async Task GetDefinitionByNameAsync_ShouldEscapeTheLikeWildcards()
		{
			await repository.GetDefinitionByNameAsync(crmMock.Object, "[DEV] Sync", null);

			Assert.IsNotNull(this.capturedQuery);
			var condition = this.capturedQuery.Criteria.Conditions.Single(c => c.AttributeName == "name");
			Assert.AreEqual("%[[]DEV] Sync%", condition.Values.Single(), "An unescaped [ would make the flow unable to match its own name.");
		}

		[TestMethod]
		public async Task SearchByNameAndSolutionAndCategoryAsync_ShouldEscapeTheLikeWildcards()
		{
			await repository.SearchByNameAndSolutionAndCategoryAsync(crmMock.Object, "100%_done", null, null);

			Assert.IsNotNull(this.capturedQuery);
			var condition = this.capturedQuery.Criteria.Conditions.Single(c => c.AttributeName == "name");
			Assert.AreEqual("%100[%][_]done%", condition.Values.Single());
		}
	}
}
