using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.PowerPlatform.Dataverse.Client;
using System.ServiceModel;

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

			var command = new DeleteCommand { SolutionUniqueName = "ExistingSolution" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync("solution", existingSolutionId, It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenSolutionNotFound_ShouldReturnFailure()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new DeleteCommand { SolutionUniqueName = "MissingSolution" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage.Contains("MissingSolution"));
			Assert.IsTrue(result.ErrorMessage.Contains("not found"));

			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithFaultException_ShouldReturnFailure()
		{
			var exception = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "Delete error" }, new FaultReason("Delete error"));
			
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ThrowsAsync(exception);

			var command = new DeleteCommand { SolutionUniqueName = "ExistingSolution" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.IsTrue(result.ErrorMessage.Contains("Delete error"));
		}
	}
}
