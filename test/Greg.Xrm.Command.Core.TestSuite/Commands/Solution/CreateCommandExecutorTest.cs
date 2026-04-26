using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class CreateCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CreateCommandExecutor executor;

		public CreateCommandExecutorTest()
		{
			this.executor = new CreateCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WhenSolutionExists_ShouldFail()
		{
			var existingSolution = new EntityCollection(new List<Entity> { new Entity("solution") });
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(existingSolution);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(existingSolution);

			var command = new CreateCommand { DisplayName = "Existing Solution" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual("A solution with the same unique name already exists", result.ErrorMessage);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithoutPublisherDetails_ShouldThrowException()
		{
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			// No publisher info provided
			var command = new CreateCommand { DisplayName = "New Solution" };

			try
			{
				await executor.ExecuteAsync(command, CancellationToken.None);
				Assert.Fail("Expected CommandException was not thrown.");
			}
			catch (CommandException ex)
			{
				Assert.IsTrue(ex.Message.Contains("publisher"));
			}
		}

		[TestMethod]
		public async Task ExecuteAsync_WithExistingPublisher_ShouldCreateSolution()
		{
			var expectedSolutionId = Guid.NewGuid();
			var existingPublisherId = Guid.NewGuid();

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			var existingPublisher = new Entity("publisher") { Id = existingPublisherId };
			var publisherCollection = new EntityCollection(new List<Entity> { existingPublisher });

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "publisher")))
				.ReturnsAsync(publisherCollection);
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "publisher"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(publisherCollection);

			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "solution")))
				.ReturnsAsync(expectedSolutionId);
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(expectedSolutionId);

			var command = new CreateCommand { DisplayName = "My Solution", PublisherUniqueName = "my_pub" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			Assert.IsInstanceOfType(result, typeof(CreateCommandResult));
			var createResult = (CreateCommandResult)result;
			Assert.AreEqual(expectedSolutionId, createResult["Solution Id"]);

			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "solution"
			)), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithNewPublisher_ShouldCreatePublisherAndSolution()
		{
			var expectedPublisherId = Guid.NewGuid();
			var expectedSolutionId = Guid.NewGuid();

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			// Publisher not found
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "publisher")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "publisher"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "publisher")))
				.ReturnsAsync(expectedPublisherId);
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "publisher"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(expectedPublisherId);

			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "solution")))
				.ReturnsAsync(expectedSolutionId);
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(expectedSolutionId);

			var command = new CreateCommand { DisplayName = "My Solution", PublisherCustomizationPrefix = "new" };
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "publisher" &&
				(string)e["customizationprefix"] == "new" &&
				(int)e["customizationoptionvalueprefix"] == 10000
			)), Times.AtLeastOnce);

			this.OrganizationServiceMock.Verify(x => x.CreateAsync(It.Is<Entity>(e =>
				e.LogicalName == "solution" &&
				((EntityReference)e["publisherid"]).Id == expectedPublisherId
			)), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithApplicationRibbons_ShouldAddRibbons()
		{
			var expectedSolutionId = Guid.NewGuid();
			var expectedPublisherId = Guid.NewGuid();

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "publisher")))
				.ReturnsAsync(new EntityCollection());
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "publisher"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection());

			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "publisher")))
				.ReturnsAsync(expectedPublisherId);
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "publisher"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(expectedPublisherId);

			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "solution")))
				.ReturnsAsync(expectedSolutionId);
			this.OrganizationServiceMock
				.Setup(x => x.CreateAsync(It.Is<Entity>(e => e.LogicalName == "solution"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(expectedSolutionId);

			// Mock Ribbon Customization retrieve
			var ribbonCustomizationId = Guid.NewGuid();
			var ribbonCustomization = new Entity("ribboncustomization") { Id = ribbonCustomizationId };
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "ribboncustomization")))
				.ReturnsAsync(new EntityCollection(new List<Entity> { ribbonCustomization }));
			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.Is<QueryExpression>(q => q.EntityName == "ribboncustomization"), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(new List<Entity> { ribbonCustomization }));

			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>()))
				.ReturnsAsync(new OrganizationResponse());

			var command = new CreateCommand
			{
				DisplayName = "My Solution",
				PublisherCustomizationPrefix = "new",
				AddApplicationRibbons = true
			};
			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);

			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.Is<AddSolutionComponentRequest>(r =>
				r.ComponentType == (int)ComponentType.RibbonCustomization &&
				r.ComponentId == ribbonCustomizationId
			)), Times.AtLeastOnce);
		}
	}
}
