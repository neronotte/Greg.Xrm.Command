using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.WebResources
{
	[Command("webresources", "applyIcons", HelpText = "(Preview) Applies icons to custom tables, starting from a given solution")]
	[Alias("wr", "apply", "icons")]
	[Alias("wr", "apply-icons")]
	public class ApplyIconsCommand : ICanProvideUsageExample
	{
		[Option("table-solution", "ts", HelpText = "The name of the solution that contains the tables to update with icons. If not specified, the default solution is considered.")]
		public string? TableSolutionName { get; set; }

		[Option("wr-solution", "wrs", HelpText = "The name of the solution that contains the web resources to set. If not specified, the default solution is considered.")]
		public string? WebResourceSolutionName { get; set; }


		[Option("no-action", "nop", HelpText = "If specified, the command will not perform any action, but it will show what it would do.", DefaultValue = false)]
		public bool NoAction { get; set; } = false;



		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("This command can be used to apply SVG icons to custom tables.");
			writer.WriteLine("Only tables without an SVG icon will be affected by the command.");
			writer.WriteLine();
			writer.WriteLine("The command requires the name of the solution that contains the tables and the name of the solution that contains the web resources to set.");
			writer.WriteLine("If one of those is not explicitly provided, the default solution for the current environment is used instead.");
			writer.WriteLine();
			writer.WriteLine("The match between the table and the web resource is done by table logical name, with the following rules:");
			writer.WriteLine();
			writer.WriteLine("1. if there is a webresource called `<table_logical_name>.svg` take that one");
			writer.WriteLine("2. if there is a webresource called `<publisherprefix>_<table_logical_name>.svg` take that one");
			writer.WriteLine("3. if there is a webresource called `<publisherprefix>_/images/<table_logical_name>.svg` take that one");
			writer.WriteLine("4. if there is a webresource that ends with `/<table_logical_name>.svg` take that one");
			writer.WriteLine();
			writer.WriteLine("You can use the `--no-action` option to see what the command would do without actually performing any action.");
		}
	}
}
