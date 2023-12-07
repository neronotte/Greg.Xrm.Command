
using Greg.Xrm.Command.Commands.UnifiedRouting;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class GetAgentStatusCommandExecutorTest
	{
		[TestMethod]
        [TestCategory("Integration")]
        public void TestQuery()
		{
			var agentPrimaryEmail = "francesco.catino@avanade.com";
            var output = new OutputToConsole();
            var settingsRepository = new SettingsRepository();
            var repository = new OrganizationServiceRepository(settingsRepository);


            var executor = new GetAgentStatusCommandExecutor(output, repository);

            executor.ExecuteAsync(new GetAgentStatusCommand
            {
				AgentPrimaryEmail = agentPrimaryEmail,
				DateTimeStatus = "28/11/2023 11:00"
            }, new CancellationToken()).Wait();

		}
	}
}
