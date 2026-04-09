namespace Greg.Xrm.Command.Commands.Forms
{
	[TestClass]
	public class FormsCommandsTest
	{
		[TestMethod]
		public void FormsList_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<FormsListCommand>(
				"forms", "list",
				"-t", "contoso.onmicrosoft.com");

			Assert.AreEqual("contoso.onmicrosoft.com", command.TenantId);
			Assert.IsNull(command.OwnerId);
			Assert.AreEqual("table", command.Format);
			Assert.IsNull(command.Token);
		}

		[TestMethod]
		public void FormsList_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<FormsListCommand>(
				"forms", "list",
				"-t", "contoso.onmicrosoft.com",
				"-o", "user-id-123",
				"-f", "json",
				"--token", "token-abc");

			Assert.AreEqual("contoso.onmicrosoft.com", command.TenantId);
			Assert.AreEqual("user-id-123", command.OwnerId);
			Assert.AreEqual("json", command.Format);
			Assert.AreEqual("token-abc", command.Token);
		}

		[TestMethod]
		public void FormsResponseCount_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<FormsResponseCountCommand>(
				"forms", "response", "count",
				"-t", "contoso.onmicrosoft.com",
				"-f", "form-id-123");

			Assert.AreEqual("contoso.onmicrosoft.com", command.TenantId);
			Assert.AreEqual("form-id-123", command.FormId);
			Assert.IsNull(command.OwnerId);
			Assert.IsNull(command.Token);
		}

		[TestMethod]
		public void FormsResponsesExport_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<FormsResponsesExportCommand>(
				"forms", "responses", "export",
				"-t", "contoso.onmicrosoft.com",
				"-f", "form-id-123",
				"-o", "responses.csv");

			Assert.AreEqual("contoso.onmicrosoft.com", command.TenantId);
			Assert.AreEqual("form-id-123", command.FormId);
			Assert.AreEqual("responses.csv", command.OutputPath);
			Assert.AreEqual("csv", command.Format);
			Assert.IsNull(command.OwnerId);
			Assert.IsFalse(command.Incremental);
			Assert.IsNull(command.Token);
		}

		[TestMethod]
		public void FormsResponsesExport_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<FormsResponsesExportCommand>(
				"forms", "responses", "export",
				"-t", "contoso.onmicrosoft.com",
				"-f", "form-id-123",
				"-o", "responses.json",
				"--owner", "user-id-123",
				"--format", "json",
				"-i",
				"--token", "token-abc");

			Assert.AreEqual("contoso.onmicrosoft.com", command.TenantId);
			Assert.AreEqual("form-id-123", command.FormId);
			Assert.AreEqual("responses.json", command.OutputPath);
			Assert.AreEqual("user-id-123", command.OwnerId);
			Assert.AreEqual("json", command.Format);
			Assert.IsTrue(command.Incremental);
			Assert.AreEqual("token-abc", command.Token);
		}
	}
}
