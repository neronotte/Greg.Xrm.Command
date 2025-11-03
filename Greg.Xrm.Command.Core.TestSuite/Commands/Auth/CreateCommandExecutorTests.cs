using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Greg.Xrm.Command.Commands.Auth
{
	[TestClass]
	public class CreateCommandExecutorTests
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void Integration_Execute01()
		{
			var storage = new Storage();
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository(storage);
			var pacxProjectRepository = new PacxProjectRepository(Mock.Of<ILogger<PacxProjectRepository>>());
			var repository = new OrganizationServiceRepository(output, settingsRepository, pacxProjectRepository);
			var executor = new CreateCommandExecutor(repository, output);

			var command = new CreateCommand
			{
				Name = "test",
				ConnectionString = ""
			};

			var task = executor.ExecuteAsync(command, CancellationToken.None);

			task.Wait();

			Assert.IsNotNull(output.ToString());
		}
	}
}
