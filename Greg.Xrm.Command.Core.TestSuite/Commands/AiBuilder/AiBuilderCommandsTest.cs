namespace Greg.Xrm.Command.Commands.AiBuilder
{
	[TestClass]
	public class AiBuilderCommandsTest
	{
		[TestMethod]
		public void AiModelList_ParseWithDefaultsShouldWork()
		{
			var command = Utility.TestParseCommand<AiModelListCommand>(
				"ai", "model", "list");

			Assert.AreEqual("table", command.Format);
			Assert.IsNull(command.Status);
		}

		[TestMethod]
		public void AiModelList_ParseWithStatusFilterShouldWork()
		{
			var command = Utility.TestParseCommand<AiModelListCommand>(
				"ai", "model", "list",
				"-f", "json",
				"-s", "Completed");

			Assert.AreEqual("json", command.Format);
			Assert.AreEqual("Completed", command.Status);
		}

		[TestMethod]
		public void AiModelTrain_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AiModelTrainCommand>(
				"ai", "model", "train",
				"-m", "model-id-123");

			Assert.AreEqual("model-id-123", command.ModelId);
			Assert.IsFalse(command.Wait);
		}

		[TestMethod]
		public void AiModelTrain_ParseWithWaitShouldWork()
		{
			var command = Utility.TestParseCommand<AiModelTrainCommand>(
				"ai", "model", "train",
				"-m", "model-id-123",
				"--wait");

			Assert.IsTrue(command.Wait);
		}

		[TestMethod]
		public void AiModelPublish_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AiModelPublishCommand>(
				"ai", "model", "publish",
				"-m", "model-id-123");

			Assert.AreEqual("model-id-123", command.ModelId);
			Assert.IsFalse(command.DryRun);
		}

		[TestMethod]
		public void AiModelPublish_ParseWithDryRunShouldWork()
		{
			var command = Utility.TestParseCommand<AiModelPublishCommand>(
				"ai", "model", "publish",
				"-m", "model-id-123",
				"--dry-run");

			Assert.IsTrue(command.DryRun);
		}

		[TestMethod]
		public void AiFormProcessor_ParseWithRequiredShouldWork()
		{
			var command = Utility.TestParseCommand<AiFormProcessorConfigureCommand>(
				"ai", "form-processor", "configure",
				"-m", "model-id-123",
				"-d", "Invoice");

			Assert.AreEqual("model-id-123", command.ModelId);
			Assert.AreEqual("Invoice", command.DocumentType);
			Assert.IsNull(command.Fields);
			Assert.IsNull(command.Tables);
		}

		[TestMethod]
		public void AiFormProcessor_ParseWithAllOptionsShouldWork()
		{
			var command = Utility.TestParseCommand<AiFormProcessorConfigureCommand>(
				"ai", "form-processor", "configure",
				"-m", "model-id-123",
				"-d", "Invoice",
				"-f", "TotalAmount", "InvoiceDate",
				"-t", "LineItems");

			Assert.AreEqual("model-id-123", command.ModelId);
			Assert.AreEqual("Invoice", command.DocumentType);
			Assert.AreEqual(2, command.Fields?.Length);
			Assert.AreEqual(1, command.Tables?.Length);
		}
	}
}
