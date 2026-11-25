using System.Xml.Linq;
using System.Xml.Schema;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Ribbon;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.Ribbon
{
	[TestClass]
	public class RibbonDiffServiceTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly Mock<IRibbonComponentRepository> repository = new();
		private readonly Guid solutionId = Guid.NewGuid();
		private readonly Guid tableId = Guid.NewGuid();

		private RibbonDiffService NewService() => new(repository.Object);

		[TestMethod]
		[DataRow("account")]
		[DataRow("")]
		public void ValidatesEmptyAndCustomizedRibbonAgainstEmbeddedSchemas(string table)
		{
			var service = NewService();
			service.ValidateXml(XElement.Parse("<RibbonDiffXml/>"), table);
			service.ValidateXml(XElement.Parse("""
				<RibbonDiffXml>
				  <CustomActions><HideCustomAction HideActionId="sample.Hide" Location="Mscrm.HomepageGrid.account.MainTab" Sequence="10" /></CustomActions>
				  <CommandDefinitions>
				    <CommandDefinition Id="sample.Command">
				      <EnableRules/><DisplayRules/>
				      <Actions><JavaScriptFunction FunctionName="sample.run" Library="$webresource:sample.js" /></Actions>
				    </CommandDefinition>
				  </CommandDefinitions>
				  <RuleDefinitions><TabDisplayRules/><DisplayRules/><EnableRules/></RuleDefinitions>
				</RibbonDiffXml>
				"""), table);
			repository.VerifyNoOtherCalls();
			crm.VerifyNoOtherCalls();
		}

		[TestMethod]
		[DataRow("<RibbonDiffXml><Unknown/></RibbonDiffXml>")]
		[DataRow("<RibbonDiffXml><CustomActions><HideCustomAction Sequence='invalid'/></CustomActions></RibbonDiffXml>")]
		[DataRow("<RibbonDiffXml><CommandDefinitions><CommandDefinition><EnableRules/><DisplayRules/><Actions/></CommandDefinition></CommandDefinitions></RibbonDiffXml>")]
		[DataRow("<RibbonDiffXml><CommandDefinitions/><CustomActions/></RibbonDiffXml>")]
		[DataRow("<RibbonDiffXml xmlns='urn:unexpected'/>")]
		[DataRow("<RibbonDefinitions/>")]
		public void RejectsInvalidRibbonStructureAttributesAndNamespaces(string xml)
		{
			foreach (var table in new[] { "", "account" })
				Assert.ThrowsExactly<XmlSchemaValidationException>(() => NewService().ValidateXml(XElement.Parse(xml), table));
		}

		[TestMethod]
		public void UsesTheEntitySchemaForTableRibbons()
		{
			var ribbon = XElement.Parse("<RibbonDiffXml><RibbonNotSupported/></RibbonDiffXml>");
			NewService().ValidateXml(ribbon, "account");
			Assert.ThrowsExactly<XmlSchemaValidationException>(() => NewService().ValidateXml(ribbon, ""));
		}

		private static Entity Component(ComponentType type, Guid objectId, int? behavior = null)
		{
			var entity = new Entity("solutioncomponent", Guid.NewGuid()) { ["componenttype"] = new OptionSetValue((int)type), ["objectid"] = objectId };
			if (behavior.HasValue) entity["rootcomponentbehavior"] = new OptionSetValue(behavior.Value);
			return entity;
		}

		private Task<string?> Validate(params Entity[] components)
		{
			repository.Setup(r => r.GetSolutionComponentsAsync(crm.Object, solutionId, 7, It.IsAny<CancellationToken>()))
				.ReturnsAsync(components.Select(e => new RibbonComponent(e)).ToList());
			return NewService().ValidateSolutionAsync(crm.Object, solutionId, "RibbonDiff", tableId, ComponentType.Entity, CancellationToken.None);
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		public async Task AcceptsFiveSegmentedTablesAndAnApplicationRibbon(int behavior)
		{
			var ribbonId = Guid.NewGuid();
			var components = Enumerable.Range(0, 5).Select(i => Component(ComponentType.Entity, i == 0 ? tableId : Guid.NewGuid(), behavior)).ToList();
			components.Add(Component(ComponentType.RibbonCustomization, ribbonId));
			repository.Setup(r => r.GetApplicationRibbonIdsAsync(crm.Object, false, ribbonId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ribbonId });
			Assert.IsNull(await Validate(components.ToArray()));
		}

		[TestMethod]
		public async Task RejectsMoreThanFiveTables()
		{
			var error = await Validate(Enumerable.Range(0, 6).Select(i => Component(ComponentType.Entity, i == 0 ? tableId : Guid.NewGuid(), 1)).ToArray());
			StringAssert.Contains(error!, "more than 5 tables");
			repository.Verify(r => r.GetSolutionComponentsAsync(crm.Object, solutionId, 7, CancellationToken.None), Times.Once);
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(-1)]
		[DataRow(null)]
		public async Task RejectsAllSubcomponentsOrUnknownSegmentation(int? behavior)
		{
			StringAssert.Contains((await Validate(Component(ComponentType.Entity, tableId, behavior)))!, "all subcomponents or unknown segmentation");
		}

		[TestMethod]
		[DataRow(ComponentType.SystemForm)]
		[DataRow(ComponentType.SavedQuery)]
		[DataRow(ComponentType.WebResource)]
		public async Task RejectsUnrelatedComponents(ComponentType type)
		{
			var error = await Validate(Component(ComponentType.Entity, tableId, 1), Component(type, Guid.NewGuid()));
			StringAssert.Contains(error!, "unsupported component type");
		}

		[TestMethod]
		public async Task RejectsAMissingTargetTable()
		{
			StringAssert.Contains((await Validate(Component(ComponentType.Entity, Guid.NewGuid(), 1)))!, "does not contain the target table");
		}

		[TestMethod]
		public async Task RejectsMultipleRibbonCustomizations()
		{
			var error = await Validate(Component(ComponentType.Entity, tableId, 1), Component(ComponentType.RibbonCustomization, Guid.NewGuid()), Component(ComponentType.RibbonCustomization, Guid.NewGuid()));
			StringAssert.Contains(error!, "more than one ribbon customization");
		}

		[TestMethod]
		public async Task RejectsNonApplicationRibbon()
		{
			var ribbonId = Guid.NewGuid();
			repository.Setup(r => r.GetApplicationRibbonIdsAsync(crm.Object, false, ribbonId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Guid>());
			StringAssert.Contains((await Validate(Component(ComponentType.Entity, tableId, 2), Component(ComponentType.RibbonCustomization, ribbonId)))!, "not Application Ribbons");
		}

		[TestMethod]
		public async Task InvalidFieldReportsItsNamesAndComponentIds()
		{
			var field = Component(ComponentType.Attribute, Guid.NewGuid());
			var metadata = new StringAttributeMetadata { LogicalName = "new_code" };
			typeof(AttributeMetadata).GetProperty(nameof(AttributeMetadata.EntityLogicalName))!.SetValue(metadata, "account");
			var response = new RetrieveAttributeResponse(); response.Results["AttributeMetadata"] = metadata;
			crm.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveAttributeRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
			var error = await Validate(field);
			StringAssert.Contains(error!, "type=Attribute (2)");
			StringAssert.Contains(error!, $"objectId={field.GetAttributeValue<Guid>("objectid")}");
			StringAssert.Contains(error!, $"solutionComponentId={field.Id}");
			StringAssert.Contains(error!, "table=account");
			StringAssert.Contains(error!, "name=new_code");
		}

		[TestMethod]
		public async Task MissingFieldMetadataUsesTheEntityReferenceRootTable()
		{
			var table = Component(ComponentType.Entity, tableId, 1);
			var field = Component(ComponentType.Attribute, Guid.NewGuid());
			field["rootsolutioncomponentid"] = new EntityReference("solutioncomponent", table.Id);
			crm.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveAttributeRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Deleted field"));
			var response = new RetrieveEntityResponse(); response.Results["EntityMetadata"] = new EntityMetadata { LogicalName = "account", MetadataId = tableId };
			crm.Setup(c => c.ExecuteAsync(It.Is<RetrieveEntityRequest>(r => r.MetadataId == tableId), It.IsAny<CancellationToken>())).ReturnsAsync(response);
			var error = await Validate(table, field);
			StringAssert.Contains(error!, "type=Attribute (2)");
			StringAssert.Contains(error!, $"rootSolutionComponentId={table.Id}");
			StringAssert.Contains(error!, "table=account");
		}

		[TestMethod]
		public async Task MissingMetadataStillReportsComponentIds()
		{
			var field = Component(ComponentType.Attribute, Guid.NewGuid());
			crm.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveAttributeRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Deleted field"));
			var error = await Validate(field);
			StringAssert.Contains(error!, $"solutionComponentId={field.Id}");
			StringAssert.Contains(error!, "type=Attribute (2)");
		}

		[TestMethod]
		public async Task ResolvesTableMetadataWithCancellationToken()
		{
			using var cancellation = new CancellationTokenSource();
			var response = new RetrieveEntityResponse(); response.Results["EntityMetadata"] = new EntityMetadata { MetadataId = tableId };
			crm.Setup(c => c.ExecuteAsync(It.Is<RetrieveEntityRequest>(r => r.LogicalName == "account"), cancellation.Token)).ReturnsAsync(response);
			var result = await NewService().ResolveComponentAsync(crm.Object, "account", cancellation.Token);
			Assert.AreEqual((tableId, ComponentType.Entity), result);
		}

		[TestMethod]
		public async Task RejectsTableWithMissingMetadataId()
		{
			var response = new RetrieveEntityResponse(); response.Results["EntityMetadata"] = new EntityMetadata();
			crm.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveEntityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
			await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => NewService().ResolveComponentAsync(crm.Object, "account", CancellationToken.None));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		public async Task ApplicationRibbonMustBeUniqueInTheActiveSolution(int count)
		{
			var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
			repository.Setup(r => r.GetApplicationRibbonIdsAsync(crm.Object, true, null, CancellationToken.None)).ReturnsAsync(ids);
			if (count == 1)
				Assert.AreEqual((ids[0], ComponentType.RibbonCustomization), await NewService().ResolveComponentAsync(crm.Object, "", CancellationToken.None));
			else
				await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => NewService().ResolveComponentAsync(crm.Object, "", CancellationToken.None));
		}
	}
}
