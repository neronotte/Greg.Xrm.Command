using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[Command("optionset", "create", HelpText = "Create a new global optionset")]
	public class CreateCommand
	{
		[Option("name", "n", Order = 1, HelpText = "The display name of the global optionset.")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("schemaName", "sn", Order = 2, HelpText = "The schema name of the global optionset.\nIf not specified, is deducted from the display name.")]
		public string? SchemaName { get; set; }


		[Option("options", "o", Order = 3, HelpText = "The list of options for the attribute, as a single string separated by comma (,) or semicolon (;) or pipe.\nYou can pass also values separating using syntax \"label1:value1,label2:value2\"\nIf not provided, values will be automatically generated.")]
		[Required]
		public string? Options { get; internal set; }


		[Option("colors", "c", Order = 4, HelpText = "The list of colors for each option, in exadecimal format, as a single string separated by comma (,).")]
		public string? Colors { get; set; }

		[Option("solution", "s", Order = 5, HelpText = "The name of the unmanaged solution to which you want to add this attribute.")]
		public string? SolutionName { get; set; }
	}
}
