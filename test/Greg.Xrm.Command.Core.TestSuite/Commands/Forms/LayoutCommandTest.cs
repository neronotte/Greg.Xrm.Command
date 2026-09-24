using System.Xml.Linq;

namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class LayoutCommandTest
	{
		private const string FormXml = """
			<form>
			  <header><rows><row><cell><labels><label description="Owner" /></labels><control id="ownerid" datafieldname="ownerid" /></cell></row></rows></header>
			  <tabs>
			    <tab name="general" visible="false" ordinal="4">
			      <labels><label description="General" languagecode="1033" /></labels>
			      <columns><column width="100%"><sections><section name="details">
			        <rows><row><cell id="field_cell" visible="false"><labels><label description="Account Name" /></labels>
			          <control id="name" datafieldname="name" /></cell></row></rows>
			      </section></sections></column></columns>
			    </tab>
			    <tab name="more" visible="true"><columns><column><sections><section name="summary" /></sections></column></columns></tab>
			  </tabs>
			  <footer />
			</form>
			""";

		[TestMethod]
		public void ParseDisplayOption()
		{
			var command = Utility.TestParseCommand<LayoutCommand>("forms", "layout", "-t", "account", "-d", "names");
			Assert.AreEqual("account", command.TableName);
			Assert.AreEqual(LayoutDisplay.Names, command.Display);
		}

		[TestMethod]
		public void RenderHierarchyWithPositionsOrderAndInheritedVisibility()
		{
			var lines = FormLayoutRenderer.Render(XElement.Parse(FormXml), LayoutDisplay.Both).ToArray();
			Assert.IsTrue(lines.Any(line => line.StartsWith("|-- Header (visible)")));
			Assert.IsTrue(lines.Any(line => line.Contains("Field Owner [name=ownerid] [row=1, cell=1] (visible)")));
			Assert.IsTrue(lines.Any(line => line.Contains("Tab General [name=general] [tab=1, ordinal=4] (hidden)")));
			Assert.IsTrue(lines.Any(line => line.Contains("Tab more [tab=2] (visible)")));
			Assert.IsTrue(lines.Any(line => line.Contains("Section details [column=1, section=1] (hidden: parent)")));
			Assert.IsTrue(lines.Any(line => line.Contains("Field Account Name [name=name] [row=1, cell=1] (hidden: parent)")));
			Assert.IsTrue(lines.Any(line => line.Contains("`-- Footer (visible)")));
			Assert.IsFalse(lines.Any(line => line.Contains("Column ") || line.Contains("Row ") || line.Contains("Cell ")));
		}

		[TestMethod]
		public void RenderRespectsLabelAndNameModes()
		{
			var form = XElement.Parse(FormXml);
			var labels = string.Join('\n', FormLayoutRenderer.Render(form, LayoutDisplay.Labels));
			var names = string.Join('\n', FormLayoutRenderer.Render(form, LayoutDisplay.Names));
			StringAssert.Contains(labels, "Field Account Name [row=1, cell=1]");
			Assert.IsFalse(labels.Contains("name=name"));
			StringAssert.Contains(names, "Field name [row=1, cell=1]");
			Assert.IsFalse(names.Contains("Account Name"));
		}
	}
}
