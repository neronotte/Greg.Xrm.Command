using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionModel = Greg.Xrm.Command.Model.Solution;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class SolutionRepositoryTest : CommandExecutorTestBase
	{
		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("Custom")]
		public async Task CreateHoldingSolutionAsync_ShouldUsePublisherOfExplicitOrDefaultSolution(string? name)
		{
			var expectedName = string.IsNullOrWhiteSpace(name) ? "Default" : name;
			OrganizationServiceRepositoryMock.Setup(repo => repo.GetCurrentDefaultSolutionAsync()).ReturnsAsync("Default");
			var publisher = new EntityReference("publisher", Guid.NewGuid());
			var source = new Entity("solution", Guid.NewGuid());
			source["publisherid"] = publisher;
			OrganizationServiceMock.Setup(crm => crm.RetrieveMultipleAsync(It.Is<QueryExpression>(query =>
				query.EntityName == "solution" && query.Criteria.Conditions.Any(condition =>
					condition.AttributeName == "uniquename" && Equals(condition.Values[0], expectedName)))))
				.ReturnsAsync(new EntityCollection([source]));
			OrganizationServiceMock.Setup(crm => crm.CreateAsync(It.IsAny<Entity>())).ReturnsAsync(Guid.NewGuid());
			var repository = new SolutionModel.Repository(Output, OrganizationServiceRepositoryMock.Object);

			var (result, solution) = await repository.CreateHoldingSolutionAsync(OrganizationServiceMock.Object, name);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNotNull(solution);
			OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentDefaultSolutionAsync(), string.IsNullOrWhiteSpace(name) ? Times.Once : Times.Never);
			OrganizationServiceMock.Verify(crm => crm.CreateAsync(It.Is<Entity>(entity =>
				entity.LogicalName == "solution" && entity.GetAttributeValue<EntityReference>("publisherid") == publisher &&
				!entity.GetAttributeValue<bool>("ismanaged"))), Times.Once);
			solution.Dispose();
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow(" ")]
		public async Task CreateHoldingSolutionAsync_WhenDefaultSolutionIsMissing_ShouldNotCreateAnything(string? defaultName)
		{
			OrganizationServiceRepositoryMock.Setup(repo => repo.GetCurrentDefaultSolutionAsync()).ReturnsAsync(defaultName);
			var repository = new SolutionModel.Repository(Output, OrganizationServiceRepositoryMock.Object);

			var (result, solution) = await repository.CreateHoldingSolutionAsync(OrganizationServiceMock.Object);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsNull(solution);
			StringAssert.Contains(result.ErrorMessage, "No solution name provided");
			OrganizationServiceMock.Verify(crm => crm.RetrieveMultipleAsync(It.IsAny<QueryBase>()), Times.Never);
			OrganizationServiceMock.Verify(crm => crm.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}

		[TestMethod]
		public async Task CreateHoldingSolutionAsync_WhenSourceSolutionIsMissing_ShouldNotCreateAnything()
		{
			OrganizationServiceMock.Setup(crm => crm.RetrieveMultipleAsync(It.IsAny<QueryBase>())).ReturnsAsync(new EntityCollection());
			var repository = new SolutionModel.Repository(Output, OrganizationServiceRepositoryMock.Object);

			var (result, solution) = await repository.CreateHoldingSolutionAsync(OrganizationServiceMock.Object, "Missing");

			Assert.IsFalse(result.IsSuccess);
			Assert.IsNull(solution);
			StringAssert.Contains(result.ErrorMessage, "Solution Missing not found");
			OrganizationServiceRepositoryMock.Verify(repo => repo.GetCurrentDefaultSolutionAsync(), Times.Never);
			OrganizationServiceMock.Verify(crm => crm.CreateAsync(It.IsAny<Entity>()), Times.Never);
		}
	}
}
