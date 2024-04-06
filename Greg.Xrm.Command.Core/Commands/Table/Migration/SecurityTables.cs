namespace Greg.Xrm.Command.Commands.Table.Migration
{
	static class SecurityTables
	{
		public static string[] SecurityTableNames { get; } = new[] { "systemuser", "businessunit", "team", "organization", "fieldsecurityprofile", "position" };
	}
}
