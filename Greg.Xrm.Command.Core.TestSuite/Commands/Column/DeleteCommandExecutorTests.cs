using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
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
			var storage = new Storage();
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository(storage);
			var repository = new OrganizationServiceRepository(settingsRepository);

			var logger = Mock.Of<ILogger<Dependency.Repository>>();

			var dependencyRepository = new Dependency.Repository(logger);
			var workflowRepository = new Workflow.Repository();
			var processTriggerRepository = new ProcessTrigger.Repository();
			var savedQueryRepository = new SavedQuery.Repository();
			var userQueryRepository = new UserQuery.Repository();
			var attributeDeletionService = new AttributeDeletionService(
				output, 
				savedQueryRepository, 
				userQueryRepository, 
				workflowRepository, 
				processTriggerRepository);

			
			var command = new DeleteCommand
			{
				TableName = "ava_table1",
				SchemaName = "ava_category",
				Force = true,
			};

			var executor = new DeleteCommandExecutor(output, repository, dependencyRepository, attributeDeletionService);

			var task = executor.ExecuteAsync(command, CancellationToken.None);

			task.Wait();
			Assert.IsFalse(task.IsFaulted, task.Exception?.Message);
			
			var result = task.Result;
			Assert.IsNotNull(result);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

			Assert.IsNotNull(output.ToString());
		}
	}
}
