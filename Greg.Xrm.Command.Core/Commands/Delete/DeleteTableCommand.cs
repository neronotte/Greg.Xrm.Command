namespace Greg.Xrm.Command.Commands.Delete
{
	[Command("delete", "table", HelpText = "Deletes a table (if possible) from the current Dataverse environment")]
	public class DeleteTableCommand
	{
		[Option("name", "n", IsRequired = true, HelpText = "The schema name of the table to delete")]
        public string? SchemaName { get; set; }
    }
}
