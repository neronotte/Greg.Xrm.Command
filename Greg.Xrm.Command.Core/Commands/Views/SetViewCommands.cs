using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	public abstract class SetViewCommand
	{
		[Option("name", "n", Order = 1, HelpText = "The display name of the view to update.")]
		[Required]
		public string ViewName { get; set; } = string.Empty;

		[Option("table", "t", Order = 2, HelpText = "The table containing the view. Required if the name is not unique.")]
		public string? TableName { get; set; }

		[Option("type", "q", Order = 3, HelpText = "The type of query.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;

		[Option("publish", "p", Order = 9, HelpText = "Publish the view's table after updating. Defaults to false.", DefaultValue = false)]
		public bool Publish { get; set; }
	}

	[Command("view", "setFilter", HelpText = "Replaces the main entity filter of a view with a FetchXML filter.")]
	public class SetFilterCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("filter", "f", Order = 4, HelpText = "A FetchXML <filter> element.")]
		[Required]
		public string Filter { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Replaces every `<filter>` directly under the view's main FetchXML `<entity>` with the supplied filter. Filters inside `<link-entity>` elements remain in place. Columns, sorting, links, and LayoutXML are preserved. The updated view is saved and left unpublished; use `--publish true` to publish its table.");
			writer.WriteParagraph("Pass one complete `<filter>` element. Nested filters and conditions inside it are supported. An invalid XML fragment or a root element other than `<filter>` is rejected before the view is saved.");
			writer.WriteCodeBlock("pacx view setFilter --table account --name \"Active Accounts\" --filter '<filter type=\"and\"><condition attribute=\"statecode\" operator=\"eq\" value=\"0\" /></filter>'", "Bash");
			writer.WriteParagraph("Use `pacx view get --table account --name \"Active Accounts\"` to inspect the resulting FetchXML.");
		}
	}

	[Command("view", "setFetchXml", HelpText = "Sets the FetchXML and displayed columns of a view.")]
	public class SetFetchXmlCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("fetchxml", "f", Order = 4, HelpText = "A complete FetchXML <fetch> document.")]
		[Required]
		public string FetchXml { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Replaces the complete FetchXML, including its filters, sorting, and linked entities. The main `<entity name>` must match the view's table. The command also rebuilds the displayed columns from `<attribute>` elements in their document order. An attribute in a direct `<link-entity alias=\"...\">` becomes a column named `alias.attribute`. The row ID attribute from LayoutXML is not displayed.");
			writer.WriteParagraph("Existing columns keep their entire `<cell>` definition, including width and other settings. New columns get width `100`. Columns absent from the new FetchXML are removed from the layout. Grid, row, and custom control settings outside the cells are preserved. The updated view is saved and left unpublished; use `--publish true` to publish its table.");
			writer.WriteParagraph("Include every filter and sort rule you want to keep in the supplied FetchXML. The command requires explicit `<attribute>` elements for displayed columns; it does not derive columns from `<all-attributes/>`.");
			writer.WriteCodeBlock("pacx view setFetchXml --table account --name \"Active Accounts\" --fetchxml '<fetch><entity name=\"account\"><attribute name=\"name\"/><attribute name=\"telephone1\"/><filter><condition attribute=\"statecode\" operator=\"eq\" value=\"0\"/></filter></entity></fetch>'", "Bash");
		}
	}

	[Command("view", "setColumns", HelpText = "Sets the displayed columns of a view.")]
	public class SetColumnsCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("columns", "c", Order = 4, HelpText = "Comma-separated logical attribute names in display order.")]
		[Required]
		public string Columns { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Sets the layout cells to the specified comma-separated attributes in exactly that order. Existing cells keep their width and other settings; new cells get width `100`. Columns omitted from the list are removed from the layout.");
			writer.WriteParagraph("The command replaces `<attribute>` elements of the main FetchXML entity with the selected main-table attributes. It retains the row ID attribute if it was already present. For linked columns, use `alias.attribute`; the alias must already exist on a direct `<link-entity>` in the view's FetchXML. Each such link's selected attributes are updated, while the link itself remains. Filters, sorting, and other FetchXML elements stay in place. The updated view is saved and left unpublished; use `--publish true` to publish its table.");
			writer.WriteCodeBlock("pacx view setColumns --table account --name \"Active Accounts\" --columns name,telephone1,createdon", "Bash");
			writer.WriteParagraph("To include a linked column, for example, use `--columns name,primarycontact.firstname` when the FetchXML has a direct link with `alias=\"primarycontact\"`.");
		}
	}

	[Command("view", "set", HelpText = "Sets both FetchXML and LayoutXML of a view.")]
	public class SetCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("layoutxml", "l", Order = 4, HelpText = "A complete LayoutXML <grid> document.")]
		[Required]
		public string LayoutXml { get; set; } = string.Empty;

		[Option("fetchxml", "f", Order = 5, HelpText = "A complete FetchXML <fetch> document to set together with the layout.")]
		[Required]
		public string FetchXml { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Replaces the view's FetchXML and LayoutXML together. The command checks that every layout `<cell name>` is selected by the supplied FetchXML. Linked cells use `alias.attribute`, matching a linked entity alias and selected attribute. A layout with missing query columns is rejected before anything is saved.");
			writer.WriteParagraph("Selected FetchXML attributes that are not shown as layout cells produce a warning. The row ID attribute is excluded from this warning. The command does not remove unused attributes automatically. By default the update is left unpublished; use `--publish true` to publish the view's table after saving. This publishes the table's customizations, not all customizations in the environment.");
			writer.WriteCodeBlock("pacx view set --table account --name \"Active Accounts\" --layoutxml '<grid name=\"resultset\" object=\"1\" jump=\"name\"><row name=\"result\" id=\"accountid\"><cell name=\"name\" width=\"150\"/><cell name=\"telephone1\" width=\"100\"/></row></grid>' --fetchxml '<fetch><entity name=\"account\"><attribute name=\"name\"/><attribute name=\"telephone1\"/><attribute name=\"accountid\"/></entity></fetch>'", "Bash");
		}
	}
}
