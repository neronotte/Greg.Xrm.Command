using System.Xml.Linq;
using Greg.Xrm.Command.Commands.Views.Model;

namespace Greg.Xrm.Command.Commands.Views
{
	[TestClass]
	public class ViewXmlEditorTest
	{
		private const string Layout = "<grid jump='name'><row id='accountid'><cell name='name' width='150' disableSorting='1'/><cell name='old' width='200'/><cell name='alias.city' width='125'/></row><controlDescriptions/></grid>";
		private const string Fetch = "<fetch><entity name='account'><attribute name='old'/><attribute name='name'/><attribute name='accountid'/><order attribute='name'/><filter type='and'><condition attribute='statecode' operator='eq' value='0'/></filter><link-entity name='contact' alias='alias'><attribute name='city'/><filter><condition attribute='statecode' operator='eq' value='1'/></filter></link-entity></entity></fetch>";

		[TestMethod]
		public void SetFilter_ReplacesOnlyMainEntityFilter()
		{
			var xml = XDocument.Parse(ViewXmlEditor.SetFilter(Fetch, "<filter type='or'><condition attribute='name' operator='not-null'/></filter>"));
			var entity = xml.Root!.Element("entity")!;
			Assert.AreEqual("or", (string?)entity.Element("filter")!.Attribute("type"));
			Assert.AreEqual("name", (string?)entity.Element("filter")!.Element("condition")!.Attribute("attribute"));
			Assert.IsNotNull(entity.Element("link-entity")!.Element("filter"));
		}

		[TestMethod]
		public void SetFetchXml_UsesAttributeOrderAndKeepsCellProperties()
		{
			var replacement = "<fetch><entity name='account'><attribute name='new'/><attribute name='name'/><link-entity name='contact' alias='alias'><attribute name='city'/></link-entity><attribute name='accountid'/></entity></fetch>";
			var (fetchXml, layoutXml) = ViewXmlEditor.SetFetchXml(Layout, replacement, "account");
			var cells = XDocument.Parse(layoutXml).Descendants("cell").ToList();
			CollectionAssert.AreEqual(new[] { "new", "name", "alias.city" }, cells.Select(c => (string?)c.Attribute("name")).ToArray());
			Assert.AreEqual("100", (string?)cells[0].Attribute("width"));
			Assert.AreEqual("150", (string?)cells[1].Attribute("width"));
			Assert.AreEqual("1", (string?)cells[1].Attribute("disableSorting"));
			Assert.AreEqual("125", (string?)cells[2].Attribute("width"));
			Assert.IsNotNull(XDocument.Parse(layoutXml).Descendants("controlDescriptions").SingleOrDefault());
			Assert.AreEqual("new", (string?)XDocument.Parse(fetchXml).Descendants("attribute").First().Attribute("name"));
		}

		[TestMethod]
		public void SetColumns_ChangesDisplayAndRootQueryAttributes()
		{
			var (fetchXml, layoutXml) = ViewXmlEditor.SetColumns(Fetch, Layout, "name, new");
			var cells = XDocument.Parse(layoutXml).Descendants("cell").ToList();
			CollectionAssert.AreEqual(new[] { "name", "new" }, cells.Select(c => (string?)c.Attribute("name")).ToArray());
			Assert.AreEqual("150", (string?)cells[0].Attribute("width"));
			Assert.AreEqual("100", (string?)cells[1].Attribute("width"));
			var entity = XDocument.Parse(fetchXml).Root!.Element("entity")!;
			CollectionAssert.AreEqual(new[] { "name", "new", "accountid" }, entity.Elements("attribute").Select(a => (string?)a.Attribute("name")).ToArray());
			Assert.IsNotNull(entity.Element("filter"));
			Assert.IsNotNull(entity.Element("order"));
		}

		[TestMethod]
		public void SetColumns_SupportsExistingLinkedEntityAliases()
		{
			var (fetchXml, layoutXml) = ViewXmlEditor.SetColumns(Fetch, Layout, "alias.city,name,alias.firstname");
			CollectionAssert.AreEqual(new[] { "alias.city", "name", "alias.firstname" },
				XDocument.Parse(layoutXml).Descendants("cell").Select(c => (string?)c.Attribute("name")).ToArray());
			Assert.AreEqual("125", (string?)XDocument.Parse(layoutXml).Descendants("cell").First().Attribute("width"));
			CollectionAssert.AreEqual(new[] { "city", "firstname" },
				XDocument.Parse(fetchXml).Descendants("link-entity").Single().Elements("attribute").Select(a => (string?)a.Attribute("name")).ToArray());
		}

		[TestMethod]
		public void CreateFromFetchXml_BuildsLayoutFromMetadata()
		{
			var (fetchXml, layoutXml) = ViewXmlEditor.CreateFromFetchXml(
				"<fetch><entity name='account'><attribute name='telephone1'/><attribute name='name'/><attribute name='accountid'/></entity></fetch>",
				"account", "accountid", "name", 1);
			var grid = XDocument.Parse(layoutXml).Root!;
			Assert.AreEqual("1", (string?)grid.Attribute("object"));
			Assert.AreEqual("name", (string?)grid.Attribute("jump"));
			Assert.AreEqual("accountid", (string?)grid.Element("row")!.Attribute("id"));
			CollectionAssert.AreEqual(new[] { "telephone1", "name" },
				grid.Descendants("cell").Select(c => (string?)c.Attribute("name")).ToArray());
			Assert.AreEqual("100", (string?)grid.Descendants("cell").First().Attribute("width"));
			Assert.AreEqual(3, XDocument.Parse(fetchXml).Descendants("attribute").Count());
		}

		[TestMethod]
		public void SetView_WarnsAboutUnshownAttributes()
		{
			const string layout = "<grid><row id='accountid'><cell name='name' width='175'/><cell name='alias.city' width='90'/></row></grid>";
			var (fetchXml, layoutXml, unused) = ViewXmlEditor.SetView(Fetch, layout, "account");
			Assert.IsTrue(fetchXml.Contains("accountid"));
			Assert.AreEqual("175", (string?)XDocument.Parse(layoutXml).Descendants("cell").First().Attribute("width"));
			CollectionAssert.AreEqual(new[] { "old" }, unused.ToArray());
		}

		[TestMethod]
		public void SetView_RejectsMissingColumns()
		{
			const string layout = "<grid><row id='accountid'><cell name='name'/><cell name='telephone1'/></row></grid>";
			var error = Assert.ThrowsExactly<ArgumentException>(() => ViewXmlEditor.SetView(Fetch, layout, "account"));
			StringAssert.Contains(error.Message, "telephone1");
			var replacement = "<fetch><entity name='account'><attribute name='name'/><attribute name='telephone1'/><attribute name='accountid'/></entity></fetch>";
			var (fetchXml, _, unused) = ViewXmlEditor.SetView(replacement, layout, "account");
			Assert.IsTrue(fetchXml.Contains("telephone1"));
			Assert.AreEqual(0, unused.Count);
		}

		[TestMethod]
		public void SetView_RejectsFetchForDifferentTable()
		{
			const string layout = "<grid><row id='accountid'><cell name='name'/></row></grid>";
			Assert.ThrowsExactly<ArgumentException>(() => ViewXmlEditor.SetView(
				"<fetch><entity name='contact'><attribute name='name'/></entity></fetch>", layout, "account"));
		}

		[TestMethod]
		public void SetView_RejectsAllAttributesBecauseCellsCannotBeChecked()
		{
			const string layout = "<grid><row id='accountid'><cell name='name'/></row></grid>";
			Assert.ThrowsExactly<ArgumentException>(() => ViewXmlEditor.SetView(
				"<fetch><entity name='account'><all-attributes/></entity></fetch>", layout, "account"));
		}
	}
}
