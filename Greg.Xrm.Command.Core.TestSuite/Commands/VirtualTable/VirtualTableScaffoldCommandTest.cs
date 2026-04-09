namespace Greg.Xrm.Command.Commands.VirtualTable
{
	[TestClass]
	public class VirtualTableScaffoldCommandTest
	{
		[TestMethod]
		public void ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<VirtualTableScaffoldCommand>(
				"virtual-table", "scaffold",
				"--datasource", "SqlServer",
				"--connection", "Server=myserver;Database=mydb;");

			Assert.AreEqual("SqlServer", command.DataSourceType);
			Assert.AreEqual("Server=myserver;Database=mydb;", command.ConnectionString);
			Assert.IsNull(command.ExternalTables);
			Assert.IsNull(command.Prefix);
			Assert.IsNull(command.SolutionUniqueName);
			Assert.IsFalse(command.DryRun);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<VirtualTableScaffoldCommand>(
				"virtual-table", "scaffold",
				"-d", "OData",
				"-c", "https://api.example.com",
				"-t", "Table1", "Table2",
				"-p", "ext",
				"-s", "MySolution",
				"--dry-run",
				"-f", "json");

			Assert.AreEqual("OData", command.DataSourceType);
			Assert.AreEqual("https://api.example.com", command.ConnectionString);
			Assert.AreEqual(2, command.ExternalTables?.Length);
			Assert.AreEqual("ext", command.Prefix);
			Assert.AreEqual("MySolution", command.SolutionUniqueName);
			Assert.IsTrue(command.DryRun);
			Assert.AreEqual("json", command.Format);
		}
	}
}
