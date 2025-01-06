using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Project;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
	[TestClass]
	public class CreateCommandExecutorTests
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void Integration_CreateGlobalOptionSetField()
		{
			var storage = new Storage();
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository(storage);
			var pacxProjectRepository = new PacxProjectRepository(Mock.Of<ILogger<PacxProjectRepository>>());
			var repository = new OrganizationServiceRepository(settingsRepository, pacxProjectRepository);

			var attributeMetadataBuilderFactory = new AttributeMetadataBuilderFactory();

			var command = new CreateCommand
			{
				EntityName = "ava_pippo",
				AttributeType = AttributeTypeCode.Picklist,
				SchemaName = "ava_test",
				DisplayName = "Test",
				GlobalOptionSetName = "ava_riccardo", //"emailserverprofile_authenticationprotocol",
				SolutionName = "master"
			};


			var executor = new CreateCommandExecutor(output, repository, attributeMetadataBuilderFactory);

			executor.ExecuteAsync(command, CancellationToken.None).Wait();

			Assert.IsNotNull(output.ToString());
		}
	}
}
