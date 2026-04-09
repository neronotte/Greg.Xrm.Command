using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greg.Xrm.Command.Plugin.Automation.Model;

namespace Greg.Xrm.Command.Plugin.Automation.Commands.Workflow
{
	public class WorkflowRunListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<WorkflowRunListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WorkflowRunListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write($"Retrieving runs for flow '{command.FlowIdentifier}'...");

			var query = new QueryExpression("flowrun");
			query.ColumnSet = new ColumnSet("name", "status", "starttime", "endtime");
			query.TopCount = command.Limit;
			query.AddOrder("starttime", OrderType.Descending);

			// We assume the identifier might be the flow ID or name.
			// This is a simplification.
			if (Guid.TryParse(command.FlowIdentifier, out var flowId))
			{
				query.Criteria.AddCondition("workflowid", ConditionOperator.Equal, flowId);
			}
			else
			{
				// Link to workflow to filter by name
				var link = query.AddLink("workflow", "workflowid", "workflowid");
				link.LinkCriteria.AddCondition("name", ConditionOperator.Equal, command.FlowIdentifier);
			}

			try
			{
				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				if (result.Entities.Count == 0)
				{
					output.WriteLine("No runs found for the specified flow.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				// Map Entity instances to DTOs — Cast<FlowRun>() would throw at runtime
				var runs = result.Entities.Select(MapToFlowRunDto).ToList();

				output.WriteTable(runs,
					() => new[] { "Start Time", "Status", "Duration (s)", "Name" },
					run => new[] {
						run.StartTime?.ToLocalTime().ToString("g") ?? "-",
						run.Status ?? "-",
						run.EndTime.HasValue && run.StartTime.HasValue ? (run.EndTime.Value - run.StartTime.Value).TotalSeconds.ToString("F1") : "-",
						run.Name ?? "-"
					}
				);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail("Error while retrieving flow runs: " + ex.Message, ex);
			}
		}

		private static FlowRun MapToFlowRunDto(Entity entity)
		{
			return new FlowRun
			{
				Name = entity.GetAttributeValue<string>("name"),
				Status = entity.GetAttributeValue<string>("status"),
				StartTime = entity.GetAttributeValue<DateTime?>("starttime"),
				EndTime = entity.GetAttributeValue<DateTime?>("endtime"),
			};
		}
	}
}
