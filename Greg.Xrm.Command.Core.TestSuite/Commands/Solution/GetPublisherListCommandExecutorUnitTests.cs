using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class GetPublisherListCommandExecutorUnitTests : CommandExecutorTestBase
	{
		private readonly GetPublisherListCommandExecutor executor;

		public GetPublisherListCommandExecutorUnitTests()
		{
			this.executor = new GetPublisherListCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithDefaultBlacklist_ShouldQueryWithDefaults()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new GetPublisherListCommand();
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => 
				q.EntityName == "publisher" &&
				q.Criteria.Conditions.Any(c => 
					c.AttributeName == "uniquename" && 
					c.Operator == ConditionOperator.NotIn && 
					c.Values.Contains("MicrosoftCorporation") &&
					c.Values.Contains("microsoftfirstparty")
				)
			), It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithCustomBlacklist_ShouldQueryWithCustomValues()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new GetPublisherListCommand { publisherBlacklist = "Custom1,Custom2" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => 
				q.EntityName == "publisher" &&
				q.Criteria.Conditions.Any(c => 
					c.AttributeName == "uniquename" && 
					c.Operator == ConditionOperator.NotIn && 
					c.Values.Contains("Custom1") &&
					c.Values.Contains("Custom2")
				)
			), It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithEmptyBlacklist_ShouldQueryWithoutBlacklistCondition()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var command = new GetPublisherListCommand { publisherBlacklist = "" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => 
				q.EntityName == "publisher" &&
				!q.Criteria.Conditions.Any(c => c.AttributeName == "uniquename" && c.Operator == ConditionOperator.NotIn)
			), It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
