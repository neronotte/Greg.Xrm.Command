using System.ComponentModel.DataAnnotations;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "setFilter", HelpText = "Replaces the main entity filter of a view with a FetchXML filter.")]
	public class SetFilterCommand : SetViewCommand, ICanProvideUsageExample
	{
		[Option("filter", "f", Order = 4, HelpText = "A FetchXML <filter> element.")]
		[Required]
		public string Filter { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Use this command to change which records an existing view shows while keeping its columns and sorting. Inspect the view with `pacx view get`, prepare the replacement filter, then apply it here. Use `view setFetchXml` when links or sorting also need to change.");
			writer.WriteParagraph("Replaces every `<filter>` directly under the view's main FetchXML `<entity>` with the supplied filter. Filters inside `<link-entity>` elements remain in place. Columns, sorting, links, and LayoutXML are preserved. The updated view is saved and left unpublished; use `--publish true` to publish its table.");
			writer.WriteParagraph("Pass one complete `<filter>` element. Nested filters and conditions inside it are supported. An invalid XML fragment or a root element other than `<filter>` is rejected before the view is saved.");
			writer.WriteCodeBlock("pacx view setFilter --table account --name \"Active Accounts\" --filter '<filter type=\"and\"><condition attribute=\"statecode\" operator=\"eq\" value=\"0\" /></filter>'", "Bash");
			writer.WriteParagraph("Use `pacx view get --table account --name \"Active Accounts\"` to inspect the resulting FetchXML.");
		}
	}
}
