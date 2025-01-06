using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class GetPublisherListCommandExecutorTest
	{
		[TestMethod]
		[TestCategory("Integration")]
		public async Task TestQuery()
		{
			var storage = new Storage();
			var output = new OutputToConsole();
			var settingsRepository = new SettingsRepository(storage);
			var pacxProjectRepository = new PacxProjectRepository(Mock.Of<ILogger<PacxProjectRepository>>());
			var repository = new OrganizationServiceRepository(settingsRepository, pacxProjectRepository);


			var executor = new GetPublisherListExecutor(output, repository);

			var result = await executor.ExecuteAsync(new GetPublisherListCommand
			{
				Verbose = true
			}, new CancellationToken());

			Assert.IsNotNull(result);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
		}
	}
}
