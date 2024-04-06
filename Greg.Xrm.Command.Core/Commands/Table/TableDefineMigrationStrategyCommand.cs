using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Table
{
    [Command("table", "defineMigrationStrategy", HelpText = "Builds the data migration strategy to populate all the tables contained in a given solution")]
	[Alias("table", "defmig")]
	public class TableDefineMigrationStrategyCommand : ICanProvideUsageExample
	{
		[Option("solution", "s", HelpText = "The name of the solution containing the entities to export. If not specified, the default solution is used instead.")]
		public string? SolutionName { get; set; }

		[Option("include-security-tables", "ist", HelpText = "If false, the security tables (organization, systemuser, businessunit, team, position, fieldsecurityprofile) are not taken consideration in the export.", DefaultValue = false)]
		public bool IncludeSecurityTables { get; set; } = false;

		[Option("skip-missing-tables", "skip", HelpText = "If true, the command will not fail if some tables are missing in the solution. The missing tables will be skipped.", DefaultValue = false)]
		public bool SkipMissingTables { get; set; } = false;

		[Option("verbose", "v", HelpText = "If true, the command will output more information about the export process.", DefaultValue = false)]
		public bool Verbose { get; set; } = false;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteLine("This command comes handy when you need to define the strategy to migrate data on a set of tables that are strictly tied together in a complex relationship graph.");
			writer.WriteLine();
			writer.Write("Starting from a set of tables contained in a given solution (if specified, otherwise it will fallback on the default solution set for this environment via `pacx solution setDefault`), it builds a directed graph ");
			writer.Write("and navigates into the graph to untangle the relationship chains, in a recursive pattern.")
			.WriteLine();
			writer.WriteLine();
			writer.WriteLine("The untangling approach has the following pattern:");
			writer.WriteLine();
			writer.WriteLine("1. Repeat while the chart contains nodes");
			writer.WriteLine("2. Extract the 'leaf nodes' from the graph (a leaf is a node that has no outbound relations - no lookup columns)");
			writer.WriteLine("3. If found");
			writer.WriteLine("    1. Each leaf node can be imported directly");
			writer.WriteLine("    2. Remove the leaf nodes from the graph, and all the relations directed towards the leaf nodes");
			writer.WriteLine("4. If not found, and there are still nodes in the graph, it means we have a cycle (a cycle is a set of nodes that are mutually related to each other)");
			writer.WriteLine("    1. Extract the 'cycles' from the graph");
			writer.WriteLine("    2. If the cycle is just one, **untangle** the cycle");
			writer.WriteLine("    3. If the cycles are more than one");
			writer.WriteLine("      1. Extract any self-loop that is also self-contained (a self contained cycle is a cycle which nodes are related exclusively to other nodes of the cycle)");
			writer.WriteLine("      2. If found, untangle those self-loops and return to bullet 1");
			writer.WriteLine("      3. If not found");
			writer.WriteLine("          1. Extract any self-contained cycle that is not a self-loop");
			writer.WriteLine("          2. If found, untangle those self-contained cycles and return to bullet 1");
			writer.WriteLine("          3. If not found, it means that there is a complex relationship graph that cannot be managed by the algorithm, and needs to be untangled manually");
			writer.WriteLine("              1. The algorithm breaks with an error, and prints the loops that he was not able to untangle");
			writer.WriteLine();
			writer.WriteLine("When we say **untangle the cycle** it means that:");
			writer.WriteLine();
			writer.WriteLine("1. if the cycle is a self-loop");
			writer.WriteLine("    1. import the table records without the lookup colum");
			writer.WriteLine("    2. the update the imported records setting the lookup column");
			writer.WriteLine("    3. remove the table from the graph");
			writer.WriteLine("2. otherwise break the chain in any point, properly managing self-loops that can be present in specific nodes of the cycle");
			writer.WriteLine("    1. once defined the breaking point, import the table without the lookup column");
			writer.WriteLine("    2. move to the next table of the chain, and import that table fully");
			writer.WriteLine("    3. repeat the process until all the tables in the chain are imported");
			writer.WriteLine("    4. update the table imported in step 2.1 with lookup columns");
			writer.WriteLine("    5. remove the tables from the graph");
			writer.WriteLine();
			writer.WriteParagraph("At the end of the execution, the command prints a sequence of commands that can be:");
			writer.WriteLine("- Full import on table 'table name'");
			writer.WriteLine("- Import table 'table name' without column(s) 'columns to ignore during the import'");
			writer.WriteLine("- Update table 'table name' to set column(s) 'columns to update'");


			writer.WriteLine("```PowerShell");
			writer.WriteLine("xrm table defineMigrationStrategy --solution \"my_solution_unique_name\"");
			writer.WriteLine("xrm table defmig --solution \"my_solution_unique_name\"");
			writer.WriteLine("```");

			writer.WriteParagraph("Use the --verbose (-v) argument to display additiona information about the sequence of operations used to build the migration strategy.");
		}
	}
}
