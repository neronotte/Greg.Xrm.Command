namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class SecurityCommandsTest
	{
		[TestMethod]
		public void AuditUser_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<SecurityAuditUserCommand>(
				"security", "audit-user",
				"-u", "user@contoso.com");

			Assert.AreEqual("user@contoso.com", command.UserIdentifier);
			Assert.AreEqual("table", command.Format);
			Assert.AreEqual("summary", command.DetailLevel);
		}

		[TestMethod]
		public void AuditUser_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<SecurityAuditUserCommand>(
				"security", "audit-user",
				"-u", "user@contoso.com",
				"-f", "json",
				"-d", "full");

			Assert.AreEqual("user@contoso.com", command.UserIdentifier);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual("full", command.DetailLevel);
		}

		[TestMethod]
		public void SharingReport_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<SecuritySharingReportCommand>(
				"security", "sharing-report",
				"-e", "account",
				"--id", "00000000-0000-0000-0000-000000000001");

			Assert.AreEqual("account", command.EntityLogicalName);
			Assert.AreEqual("00000000-0000-0000-0000-000000000001", command.RecordId);
			Assert.AreEqual("table", command.Format);
		}
	}
}
