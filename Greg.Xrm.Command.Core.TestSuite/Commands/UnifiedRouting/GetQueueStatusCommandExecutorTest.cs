using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[TestClass]
	public class GetQueueStatusCommandExecutorTest
	{
		[TestMethod]
		[TestCategory("Integration")]
		public async Task TestQuery()
		{
			var queue = "QUEUENAME";
			var storage = new Storage();
			var output = new OutputToConsole();
			var settingsRepository = new SettingsRepository(storage);
			var pacxProjectRepository = new PacxProjectRepository(Mock.Of<ILogger<PacxProjectRepository>>());
			var repository = new OrganizationServiceRepository(output, settingsRepository, pacxProjectRepository);


			var executor = new GetQueueStatusCommandExecutor(output, repository);

			var result = await executor.ExecuteAsync(new GetQueueStatusCommand
			{
				Queue = queue,
				DateTimeFilter = "28/11/2023 11:00"
			}, new CancellationToken());

			Assert.IsNotNull(result);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}
	}
}
