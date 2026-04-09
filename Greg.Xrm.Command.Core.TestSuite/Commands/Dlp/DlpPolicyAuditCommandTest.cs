namespace Greg.Xrm.Command.Commands.Dlp
{
	[TestClass]
	public class DlpPolicyAuditCommandTest
	{
		[TestMethod]
		public void ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<DlpPolicyAuditCommand>(
				"dlp", "policy-audit");

			Assert.IsNull(command.EnvironmentId);
			Assert.IsNull(command.ConnectorId);
			Assert.AreEqual("table", command.Format);
			Assert.IsFalse(command.ShowGaps);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<DlpPolicyAuditCommand>(
				"dlp", "policy-audit",
				"-e", "env-123",
				"-c", "shared_commondataservice",
				"-f", "json",
				"--show-gaps");

			Assert.AreEqual("env-123", command.EnvironmentId);
			Assert.AreEqual("shared_commondataservice", command.ConnectorId);
			Assert.AreEqual("json", command.Format);
			Assert.IsTrue(command.ShowGaps);
		}
	}
}
