
using Greg.Xrm.Command.Commands.UnifiedRouting;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

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
			var output = new OutputToConsole();
            var settingsRepository = new SettingsRepository();
            var repository = new OrganizationServiceRepository(settingsRepository);


			var executor = new GetQueueStatusCommandExecutor(output, repository);

			executor.ExecuteAsync(new GetQueueStatusCommand
            {
				Queue = queue,
				DateTimeStatus = "28/11/2023 11:00"
            }, new CancellationToken()).Wait();

		}
	}
}
