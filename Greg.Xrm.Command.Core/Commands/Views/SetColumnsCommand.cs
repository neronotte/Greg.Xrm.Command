using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "setColumns", HelpText = "Sets the displayed columns of a view.")]
	public class SetColumnsCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("columns", "c", Order = 4, HelpText = "Comma-separated logical attribute names in display order.")]
		[Required]
		public string Columns { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Use this command to choose and reorder the columns shown in an existing view. Inspect the view with `pacx view get`, then pass the desired columns in display order. Use `view set` when you need to change column widths or other layout settings.");
			writer.WriteParagraph("Sets the layout cells to the specified comma-separated attributes in exactly that order. Existing cells keep their width and other settings; new cells get width `100`. Columns omitted from the list are removed from the layout.");
			writer.WriteParagraph("The command replaces `<attribute>` elements of the main FetchXML entity with the selected main-table attributes. It retains the row ID attribute if it was already present. For linked columns, use `alias.attribute`; the alias must already exist on a direct `<link-entity>` in the view's FetchXML. Each such link's selected attributes are updated, while the link itself remains. Filters, sorting, and other FetchXML elements stay in place. The updated view is saved and left unpublished; use `--publish true` to publish its table.");
			writer.WriteCodeBlock("pacx view setColumns --table account --name \"Active Accounts\" --columns name,telephone1,createdon", "Bash");
			writer.WriteParagraph("To include a linked column, for example, use `--columns name,primarycontact.firstname` when the FetchXML has a direct link with `alias=\"primarycontact\"`.");
		}
	}
}
