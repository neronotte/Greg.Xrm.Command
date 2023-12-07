using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Auth
{
	[TestClass]
	public class CreateCommandExecutorTests
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void Integration_Execute01()
		{
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository();
			var repository = new OrganizationServiceRepository(settingsRepository);
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
