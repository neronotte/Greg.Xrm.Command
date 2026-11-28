using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;

namespace Greg.Xrm.Command.Commands.Workflows
{
	internal sealed record WorkflowLiveState(Workflow Workflow, string Dataverse, string PowerAutomate, string Check, string Reason)
	{
		public static bool IsCloudFlow(Workflow workflow) => workflow.category?.Value == (int)Workflow.Category.ModernFlow;

		public static IReadOnlyList<WorkflowLiveState> Compare(IEnumerable<Workflow> workflows, IEnumerable<PowerAutomateFlow> flows)
		{
			var byWorkflowId = new Dictionary<Guid, PowerAutomateFlow>();
			var byName = new Dictionary<Guid, PowerAutomateFlow>();
			foreach (var flow in flows)
			{
				if (Guid.TryParse(flow.Properties?.WorkflowEntityId, out var workflowId))
					byWorkflowId.TryAdd(workflowId, flow);
				if (Guid.TryParse(flow.Name, out var nameId))
					byName.TryAdd(nameId, flow);
			}

			return workflows.Select(workflow =>
			{
				if (!IsCloudFlow(workflow))
					return new WorkflowLiveState(workflow, "n/a", "n/a", "n/a", "n/a");

				var expectedState = workflow.statecode?.Value switch
				{
					(int)Workflow.State.Draft => "Stopped",
					(int)Workflow.State.Activated => "Started",
					(int)Workflow.State.Suspended => "Suspended",
					_ => null
				};
				var dataverse = expectedState is null ? "Unknown" : ((Workflow.State)workflow.statecode!.Value).ToString();
				var flow = byWorkflowId.GetValueOrDefault(workflow.Id) ?? byName.GetValueOrDefault(workflow.Id);
				var actualState = flow?.Properties?.State?.Trim();
				var powerAutomate = actualState?.ToUpperInvariant() switch
				{
					"STARTED" => "Started",
					"STOPPED" => "Stopped",
					"SUSPENDED" => "Suspended",
					_ => null
				};
				var check = expectedState is null || powerAutomate is null ? "UNKNOWN" :
					expectedState == powerAutomate ? "OK" : "MISMATCH";

				return new WorkflowLiveState(workflow, dataverse, powerAutomate ?? (flow is null ? "NotFound" : "Unknown"),
					check, flow?.Properties?.FlowSuspensionReason ?? string.Empty);
			}).ToList();
		}
	}
}
