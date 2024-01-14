
using Greg.Xrm.Command.Commands.UnifiedRouting;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class GetQueueStatusCommandExecutorTest
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void TestQuery()
		{
			var queue = "QUEUENAME";
			var storage = new Storage();
			var output = new OutputToConsole();
			var settingsRepository = new SettingsRepository(storage);
			var repository = new OrganizationServiceRepository(settingsRepository);


			var executor = new GetQueueStatusCommandExecutor(output, repository);

			executor.ExecuteAsync(new GetQueueStatusCommand
			{
				Queue = queue,
				DateTimeFilter = "28/11/2023 11:00"
			}, new CancellationToken()).Wait();

		}
	}
}
