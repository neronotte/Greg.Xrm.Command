using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class DeleteCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly DeleteCommandExecutor executor;

		public DeleteCommandExecutorTest()
		{
			this.executor = new DeleteCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenSolutionExists_ShouldDeleteSolution()
		{
			var existingSolutionId = Guid.NewGuid();
			var existingSolution = new Entity("solution") { Id = existingSolutionId };
			var collection = new EntityCollection(new List<Entity> { existingSolution });

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(collection);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(collection);

			var command = new DeleteCommand { SolutionUniqueName = "ExistingSolution" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.DeleteAsync("solution", existingSolutionId, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenSolutionNotFound_ShouldThrowException()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(new EntityCollection());

			var command = new DeleteCommand { SolutionUniqueName = "MissingSolution" };

			var result = await executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithFaultException_ShouldReturnFailure()
		{
			var exception = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "Delete error" }, new FaultReason("Delete error"));

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ThrowsAsync(exception);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ThrowsAsync(exception);

			var command = new DeleteCommand { SolutionUniqueName = "ExistingSolution" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage.Contains("Delete error"));
		}
	}
}
