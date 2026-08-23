using Greg.Xrm.Command.Commands.Views.Model;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ReplicatorTest
	{
		private const string SourceLayout =
			"<grid name=\"resultset\" object=\"1\" jump=\"name\" select=\"1\" icon=\"1\" preview=\"1\">" +
			"<row name=\"result\" id=\"accountid\">" +
			"<cell name=\"name\" width=\"300\" /><cell name=\"telephone1\" width=\"100\" />" +
			"</row></grid>";

		private const string SourceFetch =
			"<fetch version=\"1.0\"><entity name=\"account\">" +
			"<attribute name=\"name\" /><attribute name=\"telephone1\" />" +
			"<order attribute=\"name\" descending=\"false\" />" +
			"</entity></fetch>";

		private const string TargetLayoutWithCustomControl =
			"<grid name=\"resultset\" object=\"1\" jump=\"name\" select=\"1\" icon=\"1\" preview=\"1\">" +
			"<row name=\"result\" id=\"accountid\">" +
			"<cell name=\"name\" width=\"150\" />" +
			"</row>" +
			"<controlDescriptions><controlDescription forControl=\"{00000000-0000-0000-0000-000000000000}\">" +
			"<customControl name=\"MscrmControls.Grid.PcfGridControl\" />" +
			"</controlDescription></controlDescriptions></grid>";

		private const string TargetFetch =
			"<fetch version=\"1.0\"><entity name=\"account\">" +
			"<attribute name=\"name\" />" +
			"<order attribute=\"createdon\" descending=\"true\" />" +
			"</entity></fetch>";

		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();

		public ReplicatorTest()
		{
			this.crmMock
				.Setup(s => s.UpdateAsync(It.IsAny<Entity>()))
				.Returns(Task.CompletedTask);
		}

		private static SavedQuery CreateView(string name, string layoutXml, string fetchXml)
		{
			var entity = new Entity("savedquery", Guid.NewGuid());
			entity["name"] = name;
			entity["querytype"] = SavedQueryQueryType.MainApplicationView;
			entity["returnedtypecode"] = "account";
			entity["layoutxml"] = layoutXml;
			entity["fetchxml"] = fetchXml;
			return new SavedQuery(entity);
		}

		[TestMethod]
		public async Task PropagateLayout_ShouldReplaceComponentsAndSorting_ByDefault()
		{
			var source = CreateView("Source", SourceLayout, SourceFetch);
			var target = CreateView("Target", TargetLayoutWithCustomControl, TargetFetch);

			var errors = await Replicator.PropagateLayoutAsync(this.crmMock.Object, source, [target]);

			Assert.AreEqual(0, errors.Count);
			Assert.IsTrue(target.layoutxml!.Contains("telephone1"), "The column layout must be replicated.");
			Assert.IsFalse(target.layoutxml.Contains("controlDescriptions"), "By default the components of the target are replaced by the ones of the source, which has none.");
			Assert.IsTrue(target.fetchxml!.Contains("order attribute=\"name\""), "By default the sort order of the source wins.");
			Assert.IsFalse(target.fetchxml.Contains("order attribute=\"createdon\""));
		}

		[TestMethod]
		public async Task PropagateLayout_ShouldKeepTargetComponents_WhenComponentsAreExcluded()
		{
			var source = CreateView("Source", SourceLayout, SourceFetch);
			var target = CreateView("Target", TargetLayoutWithCustomControl, TargetFetch);

			var errors = await Replicator.PropagateLayoutAsync(this.crmMock.Object, source, [target], includeComponents: false);

			Assert.AreEqual(0, errors.Count);
			Assert.IsTrue(target.layoutxml!.Contains("telephone1"), "The column layout must still be replicated.");
			Assert.IsTrue(target.layoutxml.Contains("MscrmControls.Grid.PcfGridControl"), "The custom control of the target must survive.");
		}

		[TestMethod]
		public async Task PropagateLayout_ShouldKeepTargetSorting_WhenSortingIsExcluded()
		{
			var source = CreateView("Source", SourceLayout, SourceFetch);
			var target = CreateView("Target", TargetLayoutWithCustomControl, TargetFetch);

			var errors = await Replicator.PropagateLayoutAsync(this.crmMock.Object, source, [target], includeSorting: false);

			Assert.AreEqual(0, errors.Count);
			Assert.IsTrue(target.fetchxml!.Contains("order attribute=\"createdon\""), "The sort order of the target must survive.");
			Assert.IsFalse(target.fetchxml.Contains("order attribute=\"name\""));
			Assert.IsTrue(target.layoutxml!.Contains("telephone1"), "The column layout must still be replicated.");
		}
	}
}
