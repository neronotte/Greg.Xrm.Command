using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.WebResources
{
    [Command("webresources", "js", "setTemplate", HelpText = "Allows to override the default template used when creating custom JS WebResources.")]
	[Alias("wr", "js", "setTemplate")]
	[Alias("wr", "setTemplate", "js")]
	public class JsSetTemplateCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("file", "f", HelpText = "The name of the file that contains the new template")]
		[Required]
		public string FileName { get; set; } = string.Empty;

		[Option("type", "t", HelpText = "The type of the template to override.", DefaultValue = JavascriptWebResourceType.Form)]
		public JavascriptWebResourceType Type { get; set; } = JavascriptWebResourceType.Form;

		[Option("forTable", "ft", HelpText = "To be used in conjunction with `--type Ribbon`, indicates if the template is for a table command bar. If not specified, is assumed as a global command bar.", DefaultValue = false)]
		public bool ForTable { get; set; }


		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("This command can be used to override the default templates used to create JS WebResources via `pacx webresources js create`.");

			writer.WriteParagraph("PACX supports 4 different types of templates:");

			writer.WriteList(
				"**Form**: Used to create JS WebResources meant to be used in forms. (`pacx webresources js setTemplate --type Form --file ...`)",
				"**Ribbon (global)**: Used to create JS WebResources meant to be used in global command bars. (`pacx webresources js setTemplate --type Ribbon --file ...`)",
				"**Ribbon (table)**: Used to create JS WebResources meant to be used in specific table-related command bars. ( (`pacx webresources js setTemplate --type Ribbon --forTable --file ...`)",
				"**Other**: Generic template for other types of JS WebResources. (`pacx webresources js setTemplate --type Other --file ...`)");

			writer.WriteParagraph("Custom templates may contain 2 dynamic placeholders, that will be replaced with `pacx wr create js` command options:");

			writer.WriteList(
				"**%NAMESPACE%**: will be replaced with the value of `--namespace` option;",
				"**%TABLE%**: will be replaced with the value of `--table` option;"
			);
		}
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!File.Exists(this.FileName))
			{
				yield return new ValidationResult($"The specified file <{FileName}> does not exist", new[] { nameof(this.FileName) });
			}
		}
	}
}
