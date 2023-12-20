using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Solution
{
	[TestClass]
	public class GetPublisherListCommandExecutorTest
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void TestQuery()
		{
			var output = new OutputToConsole();
			var settingsRepository = new SettingsRepository();
			var repository = new OrganizationServiceRepository(settingsRepository);


			var executor = new GetPublisherListExecutor(output, repository);

			executor.ExecuteAsync(new GetPublisherListCommand
			{
				Verbose = true
			}, new CancellationToken()).Wait();

		}
	}
}
