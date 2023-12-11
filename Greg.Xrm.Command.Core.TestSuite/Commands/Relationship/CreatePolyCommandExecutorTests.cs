using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[TestClass]
	public class CreatePolyCommandExecutorTests
	{
		[TestMethod]
		[TestCategory("Integration")]
		public void Integration_CreateGlobalOptionSetField()
		{
			var output = new OutputToMemory();
			var settingsRepository = new SettingsRepository();
			var repository = new OrganizationServiceRepository(settingsRepository);


			var command = new CreatePolyCommand
			{
				ChildTable = "ava_fundedemployee",
				Parents = "ava_solutionarea,ava_practice,ava_clientbusinessgroup,ava_crossstructure",
				LookupAttributeDisplayName = "Funded By",
				RelationshipNameSuffix = "poly",
				SolutionName = "cop_solutioning"
			};


			var executor = new CreatePolyCommandExecutor(output, repository);

			var task = executor.ExecuteAsync(command, CancellationToken.None);
			Assert.IsNotNull(task);

			task.Wait();

			Assert.IsFalse(task.IsFaulted, task.Exception?.Message);

			var result = task.Result;

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsNull(result.Exception, result.Exception?.Message);
		}
	}
}
