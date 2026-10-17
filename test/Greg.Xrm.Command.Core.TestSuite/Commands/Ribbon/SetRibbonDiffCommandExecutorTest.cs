using System.IO.Compression;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
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
		public async Task ExportsAndReimportsTheSelectedSolutionWithoutCreatingOne()
		{
			var file = Path.GetTempFileName();
			var backup = Path.Combine(Path.GetTempPath(), $"ribbon-{Guid.NewGuid():N}.xml");
			try
			{
				await File.WriteAllTextAsync(file, "<RibbonDiffXml><NewAction /></RibbonDiffXml>");
				var metadataId = Guid.NewGuid();
				var metadataResponse = new RetrieveEntityResponse();
				metadataResponse.Results["EntityMetadata"] = new EntityMetadata { MetadataId = metadataId };
				var exportResponse = new ExportSolutionResponse();
				exportResponse.Results["ExportSolutionFile"] = CreateSolutionZip();
				var requests = new List<OrganizationRequest>();
				OrganizationServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
					.Callback<OrganizationRequest, CancellationToken>((request, _) => requests.Add(request))
					.ReturnsAsync((OrganizationRequest request, CancellationToken _) => request switch
					{
						RetrieveEntityRequest => metadataResponse,
						ExportSolutionRequest => exportResponse,
						_ => new OrganizationResponse()
					});

				var solutionEntity = new Entity("solution", Guid.NewGuid());
				solutionEntity["uniquename"] = "RibbonDiff";
				solutionEntity["ismanaged"] = false;
				solutionRepository.Setup(x => x.GetByUniqueNameAsync(OrganizationServiceMock.Object, "RibbonDiff"))
					.ReturnsAsync(new TestSolution(solutionEntity));
				var tableComponents = Enumerable.Range(0, 5).Select(index =>
					SolutionComponent(ComponentType.Entity, index == 0 ? metadataId : Guid.NewGuid(), 1)).ToList();
				var applicationRibbonId = Guid.NewGuid();
				tableComponents.Add(SolutionComponent(ComponentType.RibbonCustomization, applicationRibbonId));
				OrganizationServiceMock.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
					.ReturnsAsync((QueryBase query, CancellationToken _) =>
						query is QueryExpression { EntityName: "ribboncustomization" }
							? new EntityCollection([new Entity("ribboncustomization", applicationRibbonId)])
							: new EntityCollection(tableComponents));

				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);
				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand
				{
					FileName = file, TableName = "account", SolutionName = "RibbonDiff", BackupFile = backup
				}, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				StringAssert.Contains(await File.ReadAllTextAsync(backup), "OldAction");
				Assert.AreEqual("RibbonDiff", requests.OfType<ExportSolutionRequest>().Single().SolutionName);
				var imported = requests.OfType<ImportSolutionRequest>().Single().CustomizationFile;
				using var stream = new MemoryStream(imported);
				using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
				using var reader = new StreamReader(archive.GetEntry("customizations.xml")!.Open());
				StringAssert.Contains(await reader.ReadToEndAsync(), "NewAction");
				StringAssert.Contains(requests.OfType<PublishXmlRequest>().Single().ParameterXml, "<entity>account</entity>");
				solutionRepository.Verify(x => x.CreateTemporarySolutionAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), It.IsAny<EntityReference>()), Times.Never);
			}
			finally
			{
				File.Delete(file);
				File.Delete(backup);
			}
		}

		[TestMethod]
		public async Task ApplicationRibbonPublishesOnlyTheApplicationRibbon()
		{
			var file = Path.GetTempFileName();
			var backup = Path.Combine(Path.GetTempPath(), $"ribbon-{Guid.NewGuid():N}.xml");
			try
			{
				await File.WriteAllTextAsync(file, "<RibbonDiffXml><NewAction /></RibbonDiffXml>");
				var requests = RibbonDiffSolutionSetup.Setup(OrganizationServiceMock, solutionRepository, "");
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);

				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand
				{
					FileName = file, SolutionName = RibbonDiffSolutionSetup.SolutionName, BackupFile = backup
				}, CancellationToken.None);

				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.AreEqual(1, requests.OfType<ImportSolutionRequest>().Count());
				Assert.AreEqual("<importexportxml><ribbons><ribbon></ribbon></ribbons></importexportxml>", requests.OfType<PublishXmlRequest>().Single().ParameterXml);
				Assert.IsFalse(requests.OfType<PublishAllXmlRequest>().Any());
			}
			finally
			{
				File.Delete(file);
				File.Delete(backup);
			}
		}

		[TestMethod]
		[DataRow("n\n")]
		[DataRow("")]
		public async Task DeclinedOrMissingConfirmationFailsWithoutImporting(string input)
		{
			var file = Path.GetTempFileName();
			var originalInput = Console.In;
			try
			{
				await File.WriteAllTextAsync(file, "<RibbonDiffXml><NewAction /></RibbonDiffXml>");
				var requests = RibbonDiffSolutionSetup.Setup(OrganizationServiceMock, solutionRepository, "account");
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);
				Console.SetIn(new StringReader(input));

				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand
				{
					FileName = file, TableName = "account", SolutionName = RibbonDiffSolutionSetup.SolutionName
				}, CancellationToken.None);

				Assert.IsFalse(result.IsSuccess);
				StringAssert.Contains(result.ErrorMessage, "Aborted");
				Assert.IsFalse(requests.OfType<ImportSolutionRequest>().Any());
				Assert.IsFalse(requests.OfType<PublishXmlRequest>().Any());
			}
			finally
			{
				Console.SetIn(originalInput);
				File.Delete(file);
			}
		}

		private static byte[] CreateSolutionZip()
		{
			using var stream = new MemoryStream();
			using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
			using (var writer = new StreamWriter(archive.CreateEntry("customizations.xml").Open()))
				writer.Write("<ImportExportXml><Entities><Entity><Name>account</Name><RibbonDiffXml><OldAction /></RibbonDiffXml></Entity></Entities></ImportExportXml>");
			return stream.ToArray();
		}

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
						return SolutionComponent(ComponentType.Entity, Guid.NewGuid(), 1);
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
				Assert.AreEqual(7, capturedQuery?.TopCount);
				Assert.IsNotNull(capturedQuery);
				Assert.IsTrue(capturedQuery.Criteria.Conditions.Any(condition => condition.AttributeName == "solutionid"));
				Assert.IsTrue(capturedQuery.ColumnSet.Columns.Contains("componenttype"));
				Assert.IsTrue(capturedQuery.ColumnSet.Columns.Contains("rootcomponentbehavior"));
				OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.Is<ExportSolutionRequest>(_ => true), It.IsAny<CancellationToken>()), Times.Never);
				solutionRepository.Verify(x => x.CreateTemporarySolutionAsync(It.IsAny<Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2>(), It.IsAny<EntityReference>()), Times.Never);
			}
			finally
			{
				File.Delete(file);
			}
		}

		private static Entity SolutionComponent(ComponentType type, Guid objectId, int? rootBehavior = null)
		{
			var component = new Entity("solutioncomponent", Guid.NewGuid());
			component["componenttype"] = new OptionSetValue((int)type);
			component["objectid"] = objectId;
			if (rootBehavior.HasValue) component["rootcomponentbehavior"] = new OptionSetValue(rootBehavior.Value);
			return component;
		}

		[TestMethod]
		public async Task RejectsOtherComponentTypesBeforeExport()
		{
			var error = await ValidateWithoutExportAsync(id =>
				[SolutionComponent(ComponentType.Entity, id, 1), SolutionComponent(ComponentType.SystemForm, Guid.NewGuid())]);
			StringAssert.Contains(error, "unsupported component type");
		}

		[TestMethod]
		public async Task InvalidFieldReportsItsTableAndComponentIds()
		{
			var field = SolutionComponent(ComponentType.Attribute, Guid.NewGuid());
			var metadata = new StringAttributeMetadata { LogicalName = "new_code" };
			typeof(AttributeMetadata).GetProperty(nameof(AttributeMetadata.EntityLogicalName))!
				.SetValue(metadata, "account");
			var response = new RetrieveAttributeResponse();
			response.Results["AttributeMetadata"] = metadata;
			var error = await ValidateWithoutExportAsync(_ => [field], response);

			StringAssert.Contains(error!, "First invalid component: type=Attribute (2)");
			StringAssert.Contains(error!, $"objectId={field.GetAttributeValue<Guid>("objectid")}");
			StringAssert.Contains(error!, $"solutionComponentId={field.Id}");
			StringAssert.Contains(error!, "table=account");
			StringAssert.Contains(error!, "name=new_code");
		}

		[TestMethod]
		public async Task DanglingFieldStillReportsComponentIdsWhenMetadataIsMissing()
		{
			var field = SolutionComponent(ComponentType.Attribute, Guid.NewGuid());
			var error = await ValidateWithoutExportAsync(_ => [field]);

			StringAssert.Contains(error!, "First invalid component: type=Attribute (2)");
			StringAssert.Contains(error!, $"objectId={field.GetAttributeValue<Guid>("objectid")}");
			StringAssert.Contains(error!, $"solutionComponentId={field.Id}");
		}

		[TestMethod]
		public async Task InvalidFieldUsesRootTableWhenItsMetadataIsMissing()
		{
			var field = SolutionComponent(ComponentType.Attribute, Guid.NewGuid());
			var error = await ValidateWithoutExportAsync(tableId =>
			{
				var table = SolutionComponent(ComponentType.Entity, tableId, 1);
				field["rootsolutioncomponentid"] = table.Id;
				return [table, field];
			});

			StringAssert.Contains(error, "type=Attribute (2)");
			StringAssert.Contains(error, "table=account");
		}

		[TestMethod]
		public async Task RejectsTableWithAllSubcomponentsBeforeExport()
		{
			var error = await ValidateWithoutExportAsync(id => [SolutionComponent(ComponentType.Entity, id, 0)]);
			StringAssert.Contains(error, "all subcomponents");
		}

		[TestMethod]
		public async Task RejectsTableWithUnknownSegmentationBeforeExport()
		{
			var error = await ValidateWithoutExportAsync(id => [SolutionComponent(ComponentType.Entity, id)]);
			StringAssert.Contains(error, "unknown segmentation");
		}

		[TestMethod]
		public async Task RejectsNonApplicationRibbonBeforeExport()
		{
			var error = await ValidateWithoutExportAsync(id =>
				[SolutionComponent(ComponentType.Entity, id, 2), SolutionComponent(ComponentType.RibbonCustomization, Guid.NewGuid())]);
			StringAssert.Contains(error, "not Application Ribbons");
		}

		private async Task<string> ValidateWithoutExportAsync(Func<Guid, Entity[]> makeComponents, RetrieveAttributeResponse? attributeResponse = null)
		{
			var file = Path.GetTempFileName();
			try
			{
				await File.WriteAllTextAsync(file, "<RibbonDiffXml />");
				var metadataId = Guid.NewGuid();
				var response = new RetrieveEntityResponse();
			response.Results["EntityMetadata"] = new EntityMetadata { MetadataId = metadataId, LogicalName = "account" };
			OrganizationServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) => request is RetrieveAttributeRequest
					? attributeResponse ?? throw new InvalidOperationException("Attribute metadata is missing")
					: response);
				var solutionEntity = new Entity("solution", Guid.NewGuid());
				solutionEntity["uniquename"] = "RibbonDiff";
				solutionRepository.Setup(x => x.GetByUniqueNameAsync(OrganizationServiceMock.Object, "RibbonDiff"))
					.ReturnsAsync(new TestSolution(solutionEntity));
				var components = makeComponents(metadataId);
				OrganizationServiceMock.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
					.ReturnsAsync((QueryBase query, CancellationToken _) =>
						query is QueryExpression { EntityName: "solutioncomponent" }
							? new EntityCollection(components)
							: new EntityCollection());
				var executor = new SetRibbonDiffCommandExecutor(Output, OrganizationServiceRepositoryMock.Object, solutionRepository.Object);
				var result = await executor.ExecuteAsync(new SetRibbonDiffCommand
				{
					FileName = file, TableName = "account", SolutionName = "RibbonDiff"
				}, CancellationToken.None);
				Assert.IsFalse(result.IsSuccess);
				OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.Is<ExportSolutionRequest>(_ => true), It.IsAny<CancellationToken>()), Times.Never);
				return result.ErrorMessage;
			}
			finally { File.Delete(file); }
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
