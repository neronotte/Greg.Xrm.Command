using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CustomApiDeleteCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CustomApiDeleteCommandExecutor executor;

		public CustomApiDeleteCommandExecutorTest()
		{
			this.executor = new CustomApiDeleteCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenExists_ShouldDelete()
		{
			var apiId = Guid.NewGuid();
			var existingApi = new Entity("customapi") { Id = apiId };
			var entityCollection = new EntityCollection(new List<Entity> { existingApi });

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(entityCollection);

			var command = new CustomApiDeleteCommand { Name = "new_MyAction" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync("customapi", apiId, It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenNotExists_ShouldFail()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new CustomApiDeleteCommand { Name = "new_NonExistent" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual("Custom API 'new_NonExistent' not found", result.ErrorMessage);
			this.OrganizationServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}
}
