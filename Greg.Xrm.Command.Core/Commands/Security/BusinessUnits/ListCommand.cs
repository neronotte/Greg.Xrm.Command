using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.BusinessUnits
{
	public enum BusinessUnitOutputFormat { Tree, Json }

	[Command("security", "businessunit", "list", HelpText = "Show the business unit hierarchy as a tree or nested JSON.")]
	[Alias("security", "bu", "list")]
	[Alias("security", "bu", "tree")]
	public class ListCommand : ICanProvideUsageExample
	{
		[Option("format", "f", Order = 1, DefaultValue = BusinessUnitOutputFormat.Tree, HelpText = "Tree or Json. Defaults to Tree. Use --nologo for JSON pipelines.")]
		[EnumDataType(typeof(BusinessUnitOutputFormat))]
		public BusinessUnitOutputFormat Format { get; set; } = BusinessUnitOutputFormat.Tree;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx security businessunit list", "Powershell");
			writer.WriteCodeBlock("pacx security bu tree", "Powershell");
			writer.WriteCodeBlock("pacx security bu list -f Json --nologo", "Powershell");
			writer.WriteParagraph("Business units are grouped under their parents. JSON contains a BusinessUnits array of roots, each with Id, Name, ParentId and nested Children. Units whose parents are not visible are shown as roots.");
		}
	}
}