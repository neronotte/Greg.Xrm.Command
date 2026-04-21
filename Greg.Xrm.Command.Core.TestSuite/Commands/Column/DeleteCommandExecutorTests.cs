using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.AttributeDeletion;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Commands.Column
{
	[TestClass]
	public class DeleteCommandExecutorTests
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void Integration_ForceDelete()
		{
			var storage = new Greg.Xrm.Command.Services.Storage();
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository(storage);
			var pacxProjectRepository = new PacxProjectRepository(Mock.Of<ILogger<PacxProjectRepository>>());
			var repository = new OrganizationServiceRepository(output, settingsRepository, pacxProjectRepository);

			var dependencyRepository = new Greg.Xrm.Command.Model.Dependency.Repository(Mock.Of<ILogger<Greg.Xrm.Command.Model.Dependency.Repository>>());
			var workflowRepository = new Greg.Xrm.Command.Model.Workflow.Repository();
			var processTriggerRepository = new Greg.Xrm.Command.Model.ProcessTrigger.Repository();
			var savedQueryRepository = new Greg.Xrm.Command.Model.SavedQuery.Repository();
			var userQueryRepository = new Greg.Xrm.Command.Model.UserQuery.Repository();
			var attributeDeletionService = new AttributeDeletionService(output, []);


			var command = new DeleteCommand
			{
				TableName = "ava_table1",
				SchemaName = "ava_category",
				Force = true,
			};

			var executor = new DeleteCommandExecutor(output, repository, dependencyRepository, attributeDeletionService);

			var task = executor.ExecuteAsync(command, CancellationToken.None);

			task.Wait();
			Assert.IsNotNull(task);
			Assert.IsFalse(task.IsFaulted, task.Exception!.Message);

			var result = task.Result;
			Assert.IsNotNull(result);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

			Assert.IsNotNull(output.ToString());
		}
	}
}
