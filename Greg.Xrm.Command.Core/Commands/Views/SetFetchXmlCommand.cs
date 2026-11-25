using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "setFetchXml", HelpText = "Sets the FetchXML and displayed columns of a view.")]
	public class SetFetchXmlCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("fetchXml", "f", Order = 4, HelpText = "A complete FetchXML <fetch> document.")]
		[Required]
		public string FetchXml { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Use this command to replace the query of an existing view and derive its displayed columns from that query. Inspect the view with `pacx view get`, edit the complete FetchXML, then apply it here. Use `view set` when you also need to supply exact column widths and other layout settings.");
			writer.WriteParagraph("Replaces the complete FetchXML, including its filters, sorting, and linked entities. The main `<entity name>` must match the view's table. The command also rebuilds the displayed columns from `<attribute>` elements in their document order. An attribute in a direct `<link-entity alias=\"...\">` becomes a column named `alias.attribute`. The row ID attribute from LayoutXML is not displayed.");
			writer.WriteParagraph("Existing columns keep their entire `<cell>` definition, including width and other settings. New columns get width `100`. Columns absent from the new FetchXML are removed from the layout. Grid, row, and custom control settings outside the cells are preserved. The updated view is saved and left unpublished; use `--publish true` to publish its table.");
			writer.WriteParagraph("Include every filter and sort rule you want to keep in the supplied FetchXML. The command requires explicit `<attribute>` elements for displayed columns; it does not derive columns from `<all-attributes/>`.");
			writer.WriteCodeBlock("pacx view setFetchXml --table account --name \"Active Accounts\" --fetchXml '<fetch><entity name=\"account\"><attribute name=\"name\"/><attribute name=\"telephone1\"/><filter><condition attribute=\"statecode\" operator=\"eq\" value=\"0\"/></filter></entity></fetch>'", "Bash");
		}
	}
}
