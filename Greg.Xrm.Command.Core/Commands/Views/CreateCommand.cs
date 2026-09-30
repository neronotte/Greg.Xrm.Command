using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "create", HelpText = "Creates a system or personal view from FetchXML.")]
	public class CreateCommand : ICanProvideUsageExample
	{
		[Option("name", "n", Order = 1, HelpText = "Display name of the new view.")]
		[Required]
		public string ViewName { get; set; } = string.Empty;

		[Option("fetchxml", "f", Order = 2, HelpText = "Complete FetchXML with explicit attributes for the displayed columns.")]
		[Required]
		public string FetchXml { get; set; } = string.Empty;

		[Option("type", "q", Order = 3, HelpText = "SavedQuery creates a system view; UserQuery creates a personal view.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;

		[Option("publish", "p", Order = 4, HelpText = "Publish the view's table after creating the view. Defaults to false.", DefaultValue = false)]
		public bool Publish { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Creates a system view (`SavedQuery`, the default) or a personal view (`UserQuery`) from a complete FetchXML document. The main `<entity name>` determines the table. The command retrieves that table's primary ID, primary name, and object type code to construct LayoutXML. Display columns follow the order of explicit `<attribute>` elements; linked columns need a direct `<link-entity alias=\"...\">`. New cells use width `100`. The primary ID is added to FetchXML when absent and used as the layout row ID rather than displayed.");
			writer.WriteParagraph("By default the table is left unpublished; use `--publish true` to publish it after creating the view.");
			writer.WriteCodeBlock("pacx view create --name \"Active Accounts\" --fetchxml '<fetch><entity name=\"account\"><attribute name=\"name\"/><attribute name=\"telephone1\"/><filter><condition attribute=\"statecode\" operator=\"eq\" value=\"0\"/></filter></entity></fetch>'", "Bash");
			writer.WriteCodeBlock("pacx view create --name \"My Accounts\" --type UserQuery --fetchxml '<fetch><entity name=\"account\"><attribute name=\"name\"/></entity></fetch>'", "Bash");
		}
	}
}
