namespace Greg.Xrm.Command.Commands.Table
{
	[Command("table", "print", HelpText = "Returns the Mermaid (https://mermaid.js.org/) classDiagram representation of the set of tables contained in a given solution")]
	public class TablePrintMermaidCommand
	{
		[Option("solution", "s", Order = 1, HelpText = "The name of the solution containing the entities to export. If not specified, the default solution is used instead.")]
		public string? SolutionName { get; set; }

		[Option("include-security-tables", "ist", Order = 2, HelpText = "If false, the security tables (organization, systemuser, businessunit, team, position, fieldsecurityprofile) are not taken consideration in the export.", DefaultValue = false)]
		public bool IncludeSecurityTables { get; set; } = false;

		[Option("skip-missing-tables", "skip", Order = 3, HelpText = "If true, the command will not fail if some tables are missing in the solution. The missing tables will be skipped.", DefaultValue = false)]
		public bool SkipMissingTables { get; set; } = false;
	}
}
