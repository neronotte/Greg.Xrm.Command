using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ReplicateCommandExecutorTest : CommandExecutorTestBase
	{
		[TestMethod]
		public async Task ExecuteAsync_ShouldReportOnlyUpdatedTargets_WhenSomeUpdatesFail()
		{
			var source = CreateView("Source");
			var updated = CreateView("Updated");
			var failed = CreateView("Failed");
			var lookup = CreateView("Lookup");
			lookup.querytype = SavedQueryQueryType.LookupView;
			var retriever = new Mock<IViewRetrieverService>();
			retriever.Setup(x => x.GetByNameAsync(this.OrganizationServiceMock.Object, QueryType1.SavedQuery, "Source", "account"))
				.ReturnsAsync((CommandResult.Success(), source));
			var views = new Mock<ISavedQueryRepository>();
			views.Setup(x => x.GetByTableNameAsync(this.OrganizationServiceMock.Object, "account"))
				.ReturnsAsync([source, updated, failed, lookup]);
			this.OrganizationServiceMock.Setup(x => x.UpdateAsync(It.IsAny<Entity>())).Returns(Task.CompletedTask);
			this.OrganizationServiceMock.Setup(x => x.UpdateAsync(It.Is<Entity>(e => e.Id == failed.Id)))
				.ThrowsAsync(new InvalidOperationException("Update failed"));
			var publisher = new Mock<IPublishXmlBuilder>();
			publisher.Setup(x => x.Build()).Returns(new PublishXmlRequest());
			var executor = new ReplicateCommandExecutor(this.OrganizationServiceRepositoryMock.Object, this.Output, retriever.Object, views.Object, publisher.Object);

			var result = await executor.ExecuteAsync(new ReplicateCommand { ViewName = "Source", TableName = "account" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			StringAssert.Contains(this.Output.ToString(), "Editor URL unavailable for view 'Updated'");
			Assert.IsFalse(this.Output.ToString().Contains("Editor URL unavailable for view 'Failed'"));
			Assert.IsFalse(this.Output.ToString().Contains("Editor URL unavailable for view 'Lookup'"));
			Assert.IsFalse(this.Output.ToString().Contains("Editor URL unavailable for view 'Source'"));
			this.OrganizationServiceMock.Verify(x => x.UpdateAsync(It.Is<Entity>(e => e.Id == updated.Id)), Times.Once);
			this.OrganizationServiceMock.Verify(x => x.UpdateAsync(It.Is<Entity>(e => e.Id == lookup.Id)), Times.Never);
		}

		private static SavedQuery CreateView(string name)
		{
			var entity = new Entity("savedquery", Guid.NewGuid())
			{
				["name"] = name,
				["returnedtypecode"] = "account",
				["querytype"] = SavedQueryQueryType.MainApplicationView,
				["layoutxml"] = "<grid><row><cell name='name' /></row></grid>",
			};
			return new SavedQuery(entity);
		}

		[TestMethod]
		public async Task ExecuteAsync_ShouldReportEarlierUpdates_WhenLaterLayoutIsInvalid()
		{
			var source = CreateView("Source");
			var updated = CreateView("Updated");
			var invalid = CreateView("Invalid");
			invalid.layoutxml = "<invalid";
			var retriever = new Mock<IViewRetrieverService>();
			retriever.Setup(x => x.GetByNameAsync(this.OrganizationServiceMock.Object, QueryType1.SavedQuery, "Source", "account"))
				.ReturnsAsync((CommandResult.Success(), source));
			var views = new Mock<ISavedQueryRepository>();
			views.Setup(x => x.GetByTableNameAsync(this.OrganizationServiceMock.Object, "account"))
				.ReturnsAsync([source, updated, invalid]);
			this.OrganizationServiceMock.Setup(x => x.UpdateAsync(It.IsAny<Entity>())).Returns(Task.CompletedTask);
			var executor = new ReplicateCommandExecutor(this.OrganizationServiceRepositoryMock.Object, this.Output, retriever.Object, views.Object, Mock.Of<IPublishXmlBuilder>());

			var result = await executor.ExecuteAsync(new ReplicateCommand { ViewName = "Source", TableName = "account" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(this.Output.ToString(), "Editor URL unavailable for view 'Updated'");
			Assert.IsFalse(this.Output.ToString().Contains("Editor URL unavailable for view 'Invalid'"));
		}
	}
}
