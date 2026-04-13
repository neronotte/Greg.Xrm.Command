namespace Greg.Xrm.Command.Commands.Workflow
{
	[TestClass]
	public class WorkflowAutomationCommandsTest
	{
		[TestMethod]
		public void WorkflowRunGetParseWithRequiredIdShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowRunGetCommand>(
				"workflow", "run", "get",
				"--id", "run-123");

			Assert.AreEqual("run-123", command.RunId);
			Assert.IsFalse(command.IncludeActions);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void WorkflowRunGetParseWithAllOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowRunGetCommand>(
				"workflow", "run", "get",
				"--id", "run-456",
				"--workflow-id", "wf-789",
				"--actions",
				"-f", "json");

			Assert.AreEqual("run-456", command.RunId);
			Assert.AreEqual("wf-789", command.WorkflowId);
			Assert.IsTrue(command.IncludeActions);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void WorkflowRunResubmitParseWithRequiredIdShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowRunResubmitCommand>(
				"workflow", "run", "resubmit",
				"--id", "run-failed-123");

			Assert.AreEqual("run-failed-123", command.RunId);
			Assert.IsFalse(command.Wait);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void WorkflowRunResubmitParseWithWaitShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowRunResubmitCommand>(
				"workflow", "run", "resubmit",
				"--id", "run-789",
				"--wait",
				"-f", "json");

			Assert.AreEqual("run-789", command.RunId);
			Assert.IsTrue(command.Wait);
			Assert.AreEqual("json", command.Format);
		}

		[TestMethod]
		public void WorkflowRunCancelParseWithRequiredIdShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowRunCancelCommand>(
				"workflow", "run", "cancel",
				"--id", "run-running-123");

			Assert.AreEqual("run-running-123", command.RunId);
			Assert.IsFalse(command.Force);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void WorkflowRunCancelParseWithForceShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowRunCancelCommand>(
				"workflow", "run", "cancel",
				"--id", "run-456",
				"--force");

			Assert.AreEqual("run-456", command.RunId);
			Assert.IsTrue(command.Force);
		}

		[TestMethod]
		public void WorkflowSetStateParseWithRequiredOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowSetStateCommand>(
				"workflow", "set-state",
				"--id", "wf-123",
				"-s", "deactivated");

			Assert.AreEqual("wf-123", command.WorkflowId);
			Assert.AreEqual("deactivated", command.State);
			Assert.AreEqual("table", command.Format);
		}

		[TestMethod]
		public void WorkflowSetStateParseWithActivatedShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<WorkflowSetStateCommand>(
				"workflow", "set-state",
				"--id", "wf-456",
				"--state", "activated",
				"-f", "json");

			Assert.AreEqual("wf-456", command.WorkflowId);
			Assert.AreEqual("activated", command.State);
			Assert.AreEqual("json", command.Format);
		}
	}
}
