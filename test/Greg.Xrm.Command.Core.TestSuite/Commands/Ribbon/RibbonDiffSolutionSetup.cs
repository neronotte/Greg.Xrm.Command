using System.IO.Compression;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Ribbon
{
	/// <summary>
	/// Mocks a valid, exportable RibbonDiff solution whose only component is the target account table,
	/// or Application Ribbons when the table name is empty. Both ribbons contain an OldAction element.
	/// </summary>
	internal static class RibbonDiffSolutionSetup
	{
		public const string SolutionName = "RibbonDiff";

		private sealed class TestSolution(Entity entity) : Greg.Xrm.Command.Model.Solution(entity);

		/// <returns>The organization requests executed against <paramref name="crm"/>.</returns>
		public static List<OrganizationRequest> Setup(
			Mock<IOrganizationServiceAsync2> crm, Mock<ISolutionRepository> solutionRepository, string tableName, bool isManaged = false)
		{
			var applicationRibbon = string.IsNullOrWhiteSpace(tableName);
			var componentId = Guid.NewGuid();
			var metadataResponse = new RetrieveEntityResponse();
			metadataResponse.Results["EntityMetadata"] = new EntityMetadata { MetadataId = componentId };
			var exportResponse = new ExportSolutionResponse();
			exportResponse.Results["ExportSolutionFile"] = CreateSolutionZip(applicationRibbon
				? "<ImportExportXml><RibbonDiffXml><OldAction /></RibbonDiffXml></ImportExportXml>"
				: "<ImportExportXml><Entities><Entity><Name>account</Name><RibbonDiffXml><OldAction /></RibbonDiffXml></Entity></Entities></ImportExportXml>");
			var requests = new List<OrganizationRequest>();
			crm.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Callback<OrganizationRequest, CancellationToken>((request, _) => requests.Add(request))
				.ReturnsAsync((OrganizationRequest request, CancellationToken _) => request switch
				{
					RetrieveEntityRequest => metadataResponse,
					ExportSolutionRequest => exportResponse,
					_ => new OrganizationResponse()
				});

			var component = new Entity("solutioncomponent", Guid.NewGuid());
			component["componenttype"] = new OptionSetValue((int)(applicationRibbon ? ComponentType.RibbonCustomization : ComponentType.Entity));
			component["objectid"] = componentId;
			if (!applicationRibbon) component["rootcomponentbehavior"] = new OptionSetValue(1);
			crm.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((QueryBase query, CancellationToken _) =>
					query is QueryExpression { EntityName: "ribboncustomization" }
						? new EntityCollection([new Entity("ribboncustomization", componentId)])
						: new EntityCollection([component]));

			var solutionEntity = new Entity("solution", Guid.NewGuid());
			solutionEntity["uniquename"] = SolutionName;
			solutionEntity["ismanaged"] = isManaged;
			solutionRepository.Setup(x => x.GetByUniqueNameAsync(crm.Object, SolutionName))
				.ReturnsAsync(new TestSolution(solutionEntity));
			return requests;
		}

		private static byte[] CreateSolutionZip(string customizationsXml)
		{
			using var stream = new MemoryStream();
			using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
			using (var writer = new StreamWriter(archive.CreateEntry("customizations.xml").Open()))
				writer.Write(customizationsXml);
			return stream.ToArray();
		}
	}
}
