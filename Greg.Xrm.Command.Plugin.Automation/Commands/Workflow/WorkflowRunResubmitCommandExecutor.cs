using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Workflow
{
	public class WorkflowRunResubmitCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<WorkflowRunResubmitCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WorkflowRunResubmitCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Get the workflow run
				var query = new QueryExpression("asyncoperation");
				query.ColumnSet.AddColumn("asyncoperationid");
				query.ColumnSet.AddColumn("workflowactivationid");
				query.ColumnSet.AddColumn("statecode");
				query.ColumnSet.AddColumn("regardingobjectid");
				query.Criteria.AddCondition("asyncoperationid", ConditionOperator.Equal, command.RunId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Workflow run '{command.RunId}' not found.");
				}

				var run = results.Entities[0];
				var state = run.GetAttributeValue<OptionSetValue>("statecode")?.Value;

				// Only failed (31) and canceled (32) runs can be resubmitted
				if (state is not (31 or 32))
				{
					return CommandResult.Fail($"Cannot resubmit workflow run in state {state}. Only failed or canceled runs can be resubmitted.");
				}

				var workflowActivationId = run.GetAttributeValue<EntityReference>("workflowactivationid");
				if (workflowActivationId == null)
				{
					return CommandResult.Fail("Workflow run has no workflow activation ID.");
				}

				output.Write($"Resubmitting workflow run {command.RunId}...");

				// Resubmit by executing the workflow again
				var request = new ExecuteWorkflowRequest
				{
					WorkflowId = workflowActivationId.Id,
					EntityId = run.GetAttributeValue<EntityReference>("regardingobjectid")?.Id ?? Guid.Empty,
				};

				var response = (ExecuteWorkflowResponse)await crm.ExecuteAsync(request, cancellationToken);

				output.WriteLine($" Done (new run ID: {response.Id})", ConsoleColor.Green);

				if (command.Wait)
				{
					output.WriteLine("Waiting for workflow to complete...");
					await WaitForCompletionAsync(crm, response.Id, cancellationToken);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error resubmitting workflow: {ex.Message}", ex);
			}
		}

		private async Task WaitForCompletionAsync(IOrganizationServiceAsync2 crm, Guid runId, CancellationToken ct)
		{
			var maxWait = TimeSpan.FromMinutes(5);
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			while (stopwatch.Elapsed < maxWait && !ct.IsCancellationRequested)
			{
				var query = new QueryExpression("asyncoperation");
				query.ColumnSet.AddColumn("statecode");
				query.Criteria.AddCondition("asyncoperationid", ConditionOperator.Equal, runId);

				var results = await crm.RetrieveMultipleAsync(query, ct);
				if (results.Entities.Count > 0)
				{
					var state = results.Entities[0].GetAttributeValue<OptionSetValue>("statecode")?.Value;
					if (state is 30 or 31 or 32) // Succeeded, Failed, Canceled
					{
						var stateText = state switch
						{
							30 => "Succeeded",
							31 => "Failed",
							32 => "Canceled",
							_ => "Unknown",
						};
						output.WriteLine($"Workflow completed: {stateText}", state == 30 ? ConsoleColor.Green : ConsoleColor.Red);
						return;
					}
				}

				output.Write(".");
				await Task.Delay(TimeSpan.FromSeconds(5), ct);
			}

			output.WriteLine();
			output.WriteLine("Warning: Workflow did not complete within timeout.", ConsoleColor.Yellow);
		}
	}
}
