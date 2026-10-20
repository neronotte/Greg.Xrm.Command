using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "set", HelpText = "Sets both FetchXML and LayoutXML of a view.")]
	public class SetCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("layoutXml", "l", Order = 4, HelpText = "A complete LayoutXML <grid> document.")]
		[Required]
		public string LayoutXml { get; set; } = string.Empty;

		[Option("fetchXml", "f", Order = 5, HelpText = "A complete FetchXML <fetch> document to set together with the layout.")]
		[Required]
		public string FetchXml { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Use this command to control both the query and the layout of an existing view, including column widths. Retrieve the current definition with `pacx view get`, edit both XML documents, then apply them together here. Use `view setFilter` or `view setColumns` for smaller changes.");
			writer.WriteParagraph("Replaces the view's FetchXML and LayoutXML together. The command checks that every layout `<cell name>` is selected by the supplied FetchXML. Linked cells use `alias.attribute`, matching a linked entity alias and selected attribute. A layout with missing query columns is rejected before anything is saved.");
			writer.WriteParagraph("Selected FetchXML attributes that are not shown as layout cells produce a warning. The row ID attribute is excluded from this warning. The command does not remove unused attributes automatically. By default the update is left unpublished; use `--publish true` to publish the view's table after saving. This publishes the table's customizations, not all customizations in the environment.");
			writer.WriteCodeBlock("pacx view set --table account --name \"Active Accounts\" --layoutXml '<grid name=\"resultset\" object=\"1\" jump=\"name\"><row name=\"result\" id=\"accountid\"><cell name=\"name\" width=\"150\"/><cell name=\"telephone1\" width=\"100\"/></row></grid>' --fetchXml '<fetch><entity name=\"account\"><attribute name=\"name\"/><attribute name=\"telephone1\"/><attribute name=\"accountid\"/></entity></fetch>'", "Bash");
		}
	}
}
