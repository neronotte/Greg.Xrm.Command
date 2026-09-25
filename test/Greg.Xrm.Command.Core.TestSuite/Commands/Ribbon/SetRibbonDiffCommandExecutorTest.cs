using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	[TestClass]
	public class SetRibbonDiffCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly Mock<ISolutionRepository> solutionRepository = new();

		private sealed class TestSolution(Entity entity) : Greg.Xrm.Command.Model.Solution(entity);

		[TestMethod]
		public async Task RejectsSolutionWithMoreThanFiveTablesBeforeExport()
		{
			var file = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(file, "<RibbonDiffXml />");
				var metadataId = Guid.NewGuid();
				var response = new RetrieveEntityResponse();
				response.Results["EntityMetadata"] = new EntityMetadata { MetadataId = metadataId };
				OrganizationServiceMock
					.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
					.ReturnsAsync(response);

				var solutionEntity = new Entity("solution", Guid.NewGuid());
				solutionEntity["publisherid"] = new EntityReference("publisher", Guid.NewGuid());
				OrganizationServiceRepositoryMock.Setup(x => x.GetCurrentDefaultSolutionAsync()).ReturnsAsync("RibbonDiff");
				solutionRepository.Setup(x => x.GetByUniqueNameAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), "RibbonDiff"))
					.ReturnsAsync(new TestSolution(solutionEntity));
				QueryExpression? capturedQuery = null;
				OrganizationServiceMock
					.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
					.Callback<QueryBase, CancellationToken>((query, _) => capturedQuery = query as QueryExpression)
					.ReturnsAsync(new EntityCollection(Enumerable.Range(0, 6).Select(_ =>
					{
						var component = new Entity("solutioncomponent", Guid.NewGuid());
						component["objectid"] = Guid.NewGuid();
						return component;
					}).ToList()));
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand
				{
					FileName = file, TableName = "account"
				}, CancellationToken.None);

				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "more than 5 tables");
				StringAssert.Contains(Output.ToString(), "Using default solution <RibbonDiff>");
				StringAssert.Contains(Output.ToString(), "--solution RibbonDiff");
				Assert.AreEqual(6, capturedQuery?.TopCount);
				Assert.IsNotNull(capturedQuery);
				Assert.IsTrue(capturedQuery.Criteria.Conditions.Any(condition =>
					condition.AttributeName == "componenttype" && (int)condition.Values[0] == (int)ComponentType.Entity));
				solutionRepository.Verify(x => x.CreateTemporarySolutionAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), It.IsAny<EntityReference>()), Times.Never);
			}
			finally
			{
				File.Delete(file);
			}
		}

		[TestMethod]
		public async Task RejectsExpandedRibbonBeforeConnecting()
		{
			var file = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(file, "<RibbonDefinitions />");
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand { FileName = file }, CancellationToken.None);

				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "RibbonDiffXml root");
				OrganizationServiceRepositoryMock.Verify(x => x.GetCurrentConnectionAsync(), Times.Never);
			}
			finally
			{
				File.Delete(file);
			}
		}

		[TestMethod]
		public async Task RejectsMissingBackupFileDirectoryBeforeConnecting()
		{
			var file = Path.GetTempFileName();
			try
			{
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);
				var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand { FileName = file, BackupFile = Path.Combine(directory, "backup.xml") }, CancellationToken.None);

				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "Backup file directory");
				OrganizationServiceRepositoryMock.Verify(x => x.GetCurrentConnectionAsync(), Times.Never);
			}
			finally
			{
				File.Delete(file);
			}
		}

		[TestMethod]
		public async Task RejectsExistingBackupFileBeforeConnecting()
		{
			var input = Path.GetTempFileName();
			var backup = Path.GetTempFileName();
			try
			{
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand { FileName = input, BackupFile = backup }, CancellationToken.None);

				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "already exists");
				OrganizationServiceRepositoryMock.Verify(x => x.GetCurrentConnectionAsync(), Times.Never);
			}
			finally
			{
				File.Delete(input);
				File.Delete(backup);
			}
		}
	}
}
