using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "component", "types", HelpText = "Lists all \"officially known\" solution component types.")]
	public class ComponentTypesCommand : ICanProvideUsageExample
	{
		[Option("format", "f", HelpText = "Output format.", DefaultValue = OutputFormat.Table)]
		public OutputFormat Format { get; set; } = OutputFormat.Table;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("You can use this command to get the correct component type code before running `pacx solution component add` command.");

			writer.WriteParagraph(
@"**Please note**: The list of component types is based on the official Microsoft documentation, but it may not be exhaustive.

The fundamental issue is that Dataverse adds new component types (for Canvas Apps, Flows, Connection References, AI Models, etc.)
by inserting rows into internal platform tables, not by updating the componenttype picklist in every org.
So the picklist always lags, and there is no public `solutioncomponentdefinition`-style table exposed via the SDK
that gives you the canonical registry.

Sometimes, for new objects, `componenttype` matches `objecttypecode`... but not always, and there's no guarantee that it will continue to do so.");
		}

		public enum OutputFormat
		{
			Table,
			Json
		}
	}
}
