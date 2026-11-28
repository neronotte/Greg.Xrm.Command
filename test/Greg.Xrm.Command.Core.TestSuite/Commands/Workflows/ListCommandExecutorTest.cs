using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class ListCommandExecutorTest
	{
		[TestMethod]
		[DataRow(0, "Stopped", "Draft", "OK")]
		[DataRow(0, "Started", "Draft", "MISMATCH")]
		[DataRow(0, "Suspended", "Draft", "MISMATCH")]
		[DataRow(1, "Started", "Activated", "OK")]
		[DataRow(1, "Stopped", "Activated", "MISMATCH")]
		[DataRow(1, "Suspended", "Activated", "MISMATCH")]
		[DataRow(2, "Suspended", "Suspended", "OK")]
		[DataRow(2, "Started", "Suspended", "MISMATCH")]
		[DataRow(2, "Stopped", "Suspended", "MISMATCH")]
		[DataRow(1, "Unexpected", "Activated", "UNKNOWN")]
		public void CompareUsesWorkflowEntityIdBeforeName(int state, string liveState, string dataverse, string check)
		{
			var workflow = CreateWorkflow(state);
			PowerAutomateFlow[] flows =
			[
				new(workflow.Id.ToString(), new("Decoy", "Started", null, null)),
				new(Guid.NewGuid().ToString(), new("Actual flow", liveState, workflow.Id.ToString(), "Reason"))
			];

			var row = WorkflowLiveState.Compare([workflow], flows).Single();

			Assert.AreEqual(dataverse, row.Dataverse);
			Assert.AreEqual(check, row.Check);
			Assert.AreEqual(liveState == "Unexpected" ? "Unknown" : liveState, row.PowerAutomate);
			Assert.AreEqual("Reason", row.Reason);
		}

		[TestMethod]
		public void CompareFallsBackToNameAndIgnoresClassicWorkflows()
		{
			var matching = CreateWorkflow(1);
			var missing = CreateWorkflow(1);
			var classic = CreateWorkflow(1, Workflow.Category.Worfklow);
			PowerAutomateFlow[] flows =
			[
				new(matching.Id.ToString(), new("Cloud flow", "Started", null, null)),
				new(classic.Id.ToString(), new("Classic workflow", "Stopped", null, null))
			];

			var rows = WorkflowLiveState.Compare([matching, missing, classic], flows);

			Assert.AreEqual("OK", rows[0].Check);
			Assert.AreEqual("NotFound", rows[1].PowerAutomate);
			Assert.AreEqual("UNKNOWN", rows[1].Check);
			Assert.AreEqual("n/a", rows[2].Dataverse);
			Assert.AreEqual("n/a", rows[2].PowerAutomate);
			Assert.AreEqual("n/a", rows[2].Check);
		}

		[TestMethod]
		[DataRow(false, "Stopped", 0, "1 flows: 0 OK, 1 MISMATCH, 0 UNKNOWN")]
		[DataRow(true, "Stopped", 1, "1 flows: 0 OK, 1 MISMATCH, 0 UNKNOWN")]
		[DataRow(true, null, 1, "1 flows: 0 OK, 0 MISMATCH, 1 UNKNOWN")]
		[DataRow(true, "Started", 0, "1 flows: 1 OK, 0 MISMATCH, 0 UNKNOWN")]
		public async Task LiveResultsReportSummaryAndExitCode(bool failOnMismatch, string? liveState, int exitCode, string summary)
		{
			var test = new Harness();
			var workflow = CreateWorkflow(1);
			test.SetSearchResults(workflow);
			var response = new RetrieveCurrentOrganizationResponse();
			response.Results["Detail"] = new OrganizationDetail { EnvironmentId = "test-environment" };
			test.Crm.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveCurrentOrganizationRequest>(), CancellationToken.None))
				.ReturnsAsync(response);

			IReadOnlyList<PowerAutomateFlow> flows = liveState is null ? [] :
				[new(workflow.Id.ToString(), new("Cloud flow", liveState, null, null))];
			test.FlowClient.Setup(c => c.ListAsync("test-environment", CancellationToken.None)).ReturnsAsync(flows);

			var result = await test.Executor.ExecuteAsync(new ListCommand
			{
				Live = true, FailOnMismatch = failOnMismatch, SolutionName = " my_solution ",
				SearchQuery = " Approval ", Category = Workflow.Category.ModernFlow
			}, CancellationToken.None);

			Assert.AreEqual(exitCode, result.ExitCode);
			Assert.AreEqual(exitCode == 0, result.IsSuccess);
			StringAssert.Contains(test.Output.ToString(), summary);
			test.Repository.Verify(r => r.SearchByNameAndSolutionAndCategoryAsync(test.Crm.Object, "Approval", "my_solution", Workflow.Category.ModernFlow), Times.Once);
			test.Crm.Verify(c => c.ExecuteAsync(It.IsAny<RetrieveCurrentOrganizationRequest>(), CancellationToken.None), Times.Once);
			test.FlowClient.Verify(c => c.ListAsync("test-environment", CancellationToken.None), Times.Once);
		}

		[TestMethod]
		[DataRow(false, Workflow.Category.ModernFlow)]
		[DataRow(true, Workflow.Category.Worfklow)]
		public async Task SkipsLiveServiceWhenNotNeeded(bool live, Workflow.Category category)
		{
			var test = new Harness();
			test.SetSearchResults(CreateWorkflow(1, category));

			var result = await test.Executor.ExecuteAsync(new ListCommand { Live = live, SolutionName = "*" }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess);
			test.Crm.VerifyNoOtherCalls();
			test.FlowClient.VerifyNoOtherCalls();
			Assert.AreEqual(live, test.Output.ToString().Contains("Power Automate", StringComparison.Ordinal));
		}

		[TestMethod]
		public async Task ReportsOrganizationLookupFaultAsLiveApiError()
		{
			var test = new Harness();
			test.SetSearchResults(CreateWorkflow(1));
			test.Crm.Setup(c => c.ExecuteAsync(It.IsAny<RetrieveCurrentOrganizationRequest>(), CancellationToken.None))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Simulated Dataverse fault"));

			var result = await test.Executor.ExecuteAsync(new ListCommand { Live = true, SolutionName = "*" }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			Assert.AreEqual(2, result.ExitCode);
			StringAssert.Contains(result.ErrorMessage, "Simulated Dataverse fault");
			test.FlowClient.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task RequiresSolutionWhenThereIsNoDefault()
		{
			var test = new Harness();

			var result = await test.Executor.ExecuteAsync(new ListCommand { Live = true }, CancellationToken.None);

			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "default solution");
			test.Repository.VerifyNoOtherCalls();
			test.Crm.VerifyNoOtherCalls();
			test.FlowClient.VerifyNoOtherCalls();
		}

		private static Workflow CreateWorkflow(int state, Workflow.Category category = Workflow.Category.ModernFlow) =>
			new(new Entity("workflow", Guid.NewGuid())
			{
				["name"] = "Example",
				["category"] = new OptionSetValue((int)category),
				["statecode"] = new OptionSetValue(state)
			});

		private sealed class Harness
		{
			public OutputToMemory Output { get; } = new();
			public Mock<IOrganizationServiceAsync2> Crm { get; } = new();
			public Mock<IWorkflowRepository> Repository { get; } = new();
			public Mock<IPowerAutomateFlowClient> FlowClient { get; } = new();
			public ListCommandExecutor Executor { get; }

			public Harness()
			{
				var connections = new Mock<IOrganizationServiceRepository>();
				connections.Setup(c => c.GetCurrentConnectionAsync()).ReturnsAsync(Crm.Object);
				Executor = new ListCommandExecutor(Output, connections.Object, Repository.Object, FlowClient.Object);
			}

			public void SetSearchResults(params Workflow[] workflows) =>
				Repository.Setup(r => r.SearchByNameAndSolutionAndCategoryAsync(Crm.Object, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Workflow.Category?>()))
					.ReturnsAsync(workflows);
		}
	}
}
