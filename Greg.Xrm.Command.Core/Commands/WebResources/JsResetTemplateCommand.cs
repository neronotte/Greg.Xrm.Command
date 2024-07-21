using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "js", "resetTemplate", HelpText = "Allows to restore the default templates used for JS WebResources to the one shipped by default by PACX.")]
	[Alias("wr", "js", "resetTemplate")]
	[Alias("wr", "resetTemplate", "js")]
	public class JsResetTemplateCommand : ICanProvideUsageExample
	{
		[Option("type", "t", HelpText = "The type of the template to restore.", DefaultValue = JavascriptWebResourceType.Form)]
		[Required(ErrorMessage = "The template type is required")]
		public JavascriptWebResourceType Type { get; set; } = JavascriptWebResourceType.Form;

		[Option("forTable", "ft", HelpText = "To be used in conjunction with `--type Ribbon`, indicates if the template is for a table command bar. If not specified, is assumed as a global command bar.", DefaultValue = false)]
		public bool ForTable { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used whenever you have overridden the default templates used to create JS WebResources via `pacx webresources js setTemplate` and you want to restore the default templates shipped by PACX. The command will restore the default templates for the specified type.");
			writer.WriteParagraph("The default templates are: ");

			writer.WriteTitle3("Form");

			writer.WriteCodeBlockStart("Javascript");
			writer.WriteLine(Properties.Resources.TemplateJsForm);
			writer.WriteCodeBlockEnd().WriteLine();

			writer.WriteTitle3("Ribbon (command bar) - Table-related");

			writer.WriteCodeBlockStart("Javascript");
			writer.WriteLine(Properties.Resources.TemplateJsRibbonTable);
			writer.WriteCodeBlockEnd().WriteLine();

			writer.WriteTitle3("Ribbon (command bar) - Global");

			writer.WriteCodeBlockStart("Javascript");
			writer.WriteLine(Properties.Resources.TemplateJsRibbonGlobal);
			writer.WriteCodeBlockEnd().WriteLine();

			writer.WriteTitle3("Other");

			writer.WriteCodeBlockStart("Javascript");
			writer.WriteLine(Properties.Resources.TemplateJsOther);
			writer.WriteCodeBlockEnd().WriteLine();

		}
	}
}
