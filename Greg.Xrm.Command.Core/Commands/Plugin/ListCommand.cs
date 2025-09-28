using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Plugin
{
	[Command("plugin", "list", HelpText = "Lists plugin steps associated with a plugin assembly, plugin type, table, or solution.")]
	public class ListCommand : ICanProvideUsageExample, IValidatableObject
	{
		[Option("assembly", "a", HelpText = "Name or GUID of the plugin assembly to filter steps by.")]
		public string? AssemblyName { get; set; }

		[Option("class", "c", HelpText = "Name or GUID of the plugin type to filter steps by. Names support partial matching for fuzzy search.")]
		public string? PluginTypeName { get; set; }

		[Option("table", "t", HelpText = "Name of the table to filter steps by (e.g., account, contact). Shows all plugin steps registered for this table.")]
		public string? TableName { get; set; }

		[Option("solution", "s", HelpText = "Name of the solution to filter steps by. Shows all plugin steps from assemblies in the specified solution. If not specified, uses the current default solution.")]
		public string? SolutionName { get; set; }

		[Option("showInternalPlugins", "all", HelpText = "Include internal system plugin steps (all stages). By default, only user-manageable stages are shown (PreValidation, PreOperation, PostOperation).")]
		public bool ShowInternalPlugins { get; set; } = false;

		[Option("format", "f", HelpText = "Output format for the results.", DefaultValue = OutputFormat.Table)]
		public OutputFormat Format { get; set; } = OutputFormat.Table;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			var parameterCount = new[] { AssemblyName, PluginTypeName, TableName, SolutionName }.Count(p => !string.IsNullOrWhiteSpace(p));
			
			if (parameterCount > 1)
			{
				yield return new ValidationResult("Cannot specify multiple filter parameters. Please provide only one of: AssemblyName, PluginTypeName, TableName, or SolutionName.", [nameof(AssemblyName), nameof(PluginTypeName), nameof(TableName), nameof(SolutionName)]);
			}
			
			// Note: If no parameters are specified, we'll use the default solution
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("The plugin list command allows you to view all plugin steps associated with a specific plugin assembly, plugin type, table, or solution. You must specify exactly one filter parameter.");

			writer.WriteTitle3("Required Filter Options");
			writer.WriteParagraph("You must specify exactly one of the following options, or omit all to use the default solution:");
			writer.WriteList(
				"`--assembly` (or `-a`): Show all steps for a specific plugin assembly (accepts assembly name or GUID)",
				"`--class` (or `-c`): Show all steps for a specific plugin type (accepts plugin type name, partial name for fuzzy search, or GUID)",
				"`--table` (or `-t`): Show all steps registered for a specific table",
				"`--solution` (or `-s`): Show all steps from assemblies in a specific solution",
				"No parameters: Show all steps from assemblies in the current default solution");
			writer.WriteLine();

			writer.WriteTitle3("Optional Parameters");
			writer.WriteParagraph("You can optionally modify the command behavior with the following options:");
			writer.WriteList(
				"`--showInternalPlugins` (or `-all`): Include internal system plugin steps in the results (default is to exclude these steps)",
				"`--format` (or `-f`): Specify the output format (default is Table)");

			writer.WriteTitle3("Output Formats");
			writer.WriteParagraph("Choose how to display the results:");
			writer.WriteList(
				"`Table`: Display results in a formatted table (default)",
				"`Json`: Display results as JSON for programmatic processing");
			writer.WriteLine();

			writer.WriteTitle3("Usage Examples");

			writer.WriteParagraph("**List all steps for a plugin assembly:**");
			writer.WriteCodeBlock(@"# List all steps in the MyPluginAssembly
pacx plugin list --assembly MyPluginAssembly

# Or using assembly GUID
pacx plugin list --assembly 12345678-1234-5678-9abc-123456789012", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**List all steps for a specific plugin type:**");
			writer.WriteCodeBlock(@"# List all steps for Account_OnPreCreate_ValidateFields plugin type
pacx plugin list --class Account_OnPreCreate_ValidateFields

# Or using plugin type GUID
pacx plugin list --class 87654321-4321-8765-cba9-210987654321", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**List all steps for a specific table:**");
			writer.WriteCodeBlock(@"# List all steps registered for the account table
pacx plugin list --table account", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**List all steps for a specific solution:**");
			writer.WriteCodeBlock(@"# List all steps from assemblies in MyCustomSolution
pacx plugin list --solution MyCustomSolution", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**List all steps from the default solution:**");
			writer.WriteCodeBlock(@"# List all steps from assemblies in the current default solution
pacx plugin list", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Fuzzy search for plugin types:**");
			writer.WriteCodeBlock(@"# List all steps for plugin types containing 'Account'
pacx plugin list --class Account", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Get results in JSON format:**");
			writer.WriteCodeBlock(@"# List all steps for a table in JSON format
pacx plugin list --table contact --format Json", "powershell");
			writer.WriteLine();

			writer.WriteParagraph("**Include internal system plugin steps:**");
			writer.WriteCodeBlock(@"# List all steps including internal system stages
pacx plugin list --table account --showInternalPlugins

# Or using short form
pacx plugin list --table account --all", "powershell");
			writer.WriteLine();

			writer.WriteTitle3("Output Information");
			writer.WriteParagraph("For each plugin step, the following information is displayed:");
			writer.WriteList(
				"**Assembly**: The plugin assembly containing the step",
				"**Plugin Type**: The plugin class that executes the step",
				"**Message**: The message that triggers the step (e.g., Create, Update, Delete)",
				"**Table**: The primary table the step is registered for (empty for global messages)",
				"**Stage**: The pipeline stage (PreValidation, PreOperation, PostOperation)",
				"**Mode**: The execution mode (Sync or Async)",
				"**Rank**: The execution order within the same stage (lower numbers execute first)",
				"**Status**: Whether the step is Active or Inactive",
				"**Images**: Pre/Post image configuration (\"pre\", \"post\", \"pre/post\", or empty)",
				"**In Solution**: Only shown when using solution-based filtering - indicates if the step itself is a component of the specified solution");
			writer.WriteLine();

			writer.WriteTitle3("Sorting Logic");
			writer.WriteParagraph("When filtering by table (`--table`), results are sorted by:");
			writer.WriteList(
				"1. **Message** (alphabetically)",
				"2. **Mode** (Sync steps first, then Async steps)",
				"3. **Stage** (PreValidation, PreOperation, PostOperation)",
				"4. **Rank** (execution order within the same stage)",
				"5. **Plugin Type Name** (alphabetically)");
			writer.WriteLine();

			writer.WriteTitle3("Important Notes");
			writer.WriteList(
				"Filter parameters are mutually exclusive - specify only one at a time",
				"Plugin type search supports partial matching - be specific if multiple matches are found",
				"Table filtering shows all plugin steps for that table across all assemblies and plugin types",
				"Solution filtering shows steps from assemblies that are components of the specified solution",
				"When no filter is specified, the current default solution is used automatically",
				"The 'In Solution' column only appears when using solution-based filtering",
				"The command shows both active and inactive plugin steps",
				"By default, only user-manageable plugin steps are shown (PreValidation, PreOperation, PostOperation stages)",
				"Use `--showInternalPlugins` or `--all` to include internal system plugin steps from all stages");
		}

		public enum OutputFormat
		{
			Table,
			Json
		}
	}
}