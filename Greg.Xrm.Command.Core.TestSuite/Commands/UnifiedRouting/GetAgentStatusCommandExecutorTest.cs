using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Commands.UnifiedRouting
{
	[TestClass]
	public class GetAgentStatusCommandExecutorTest
	{
		[TestMethod]
		[TestCategory("Integration")]
		public async Task TestQuery()
		{
			var agentPrimaryEmail = "francesco.catino@avanade.com";
			var storage = new Storage();
			var output = new OutputToConsole();
			var settingsRepository = new SettingsRepository(storage);
			var pacxProjectRepository = new PacxProjectRepository(Mock.Of<ILogger<PacxProjectRepository>>());
			var repository = new OrganizationServiceRepository(settingsRepository, pacxProjectRepository);


			var executor = new GetAgentStatusCommandExecutor(output, repository);

			var result = await executor.ExecuteAsync(new GetAgentStatusCommand
			{
				AgentPrimaryEmail = agentPrimaryEmail,
				DateTimeFilter = "28/11/2023 11:00"
			}, new CancellationToken());

			Assert.IsNotNull(result);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}
	}
}
