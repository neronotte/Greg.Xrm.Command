using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	[TestClass]
	public class CustomApiListCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CustomApiListCommandExecutor executor;

		public CustomApiListCommandExecutorTest()
		{
			this.executor = new CustomApiListCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithResults_ShouldDisplayTable()
		{
			var entities = new EntityCollection(new List<Entity>
			{
				new Entity("customapi") 
				{ 
					Id = Guid.NewGuid(),
					["uniquename"] = "new_MyAction1",
					["displayname"] = "My Action 1",
					["boundentitylogicalname"] = "account",
					["isfunction"] = true,
					["createdon"] = DateTime.Parse("2024-01-15")
				},
				new Entity("customapi")
				{
					Id = Guid.NewGuid(),
					["uniquename"] = "new_MyAction2",
					["displayname"] = "My Action 2",
					["isfunction"] = false,
					["createdon"] = DateTime.Parse("2024-02-20")
				}
			});

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(entities);

			var command = new CustomApiListCommand { Format = "table" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "new_MyAction1");
			StringAssert.Contains(this.Output.ToString(), "new_MyAction2");
			StringAssert.Contains(this.Output.ToString(), "Total:");
		}

		[TestMethod]
		public async Task ExecuteAsync_WithJsonFormat_ShouldOutputJson()
		{
			var entities = new EntityCollection(new List<Entity>
			{
				new Entity("customapi")
				{
					Id = Guid.NewGuid(),
					["uniquename"] = "new_MyAction",
					["displayname"] = "My Action",
					["description"] = "Test description",
					["isfunction"] = true
				}
			});

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(entities);

			var command = new CustomApiListCommand { Format = "json" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "UniqueName");
			StringAssert.Contains(this.Output.ToString(), "new_MyAction");
		}

		[TestMethod]
		public async Task ExecuteAsync_WithEmptyResults_ShouldShowMessage()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new CustomApiListCommand();
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "No Custom APIs found");
		}

		[TestMethod]
		public async Task ExecuteAsync_WithEntityFilter_ShouldFilterQuery()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new CustomApiListCommand { EntityLogicalName = "account" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(
				It.Is<QueryExpression>(q => 
					q.EntityName == "customapi" &&
					q.Criteria.Conditions.Any(c => c.AttributeName == "boundentitylogicalname")
				),
				It.IsAny<CancellationToken>()
			), Times.Once);
		}
	}
}
