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
	public class WorkflowSetStateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<WorkflowSetStateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WorkflowSetStateCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Validate state
				var targetState = command.State.ToLower() switch
				{
					"activated" or "active" or "on" => 1,
					"deactivated" or "inactive" or "off" => 0,
					_ => -1,
				};

				if (targetState < 0)
				{
					return CommandResult.Fail($"Invalid state '{command.State}'. Use 'activated' or 'deactivated'.");
				}

				// Get the workflow
				var query = new QueryExpression("workflow");
				query.ColumnSet.AddColumn("workflowid");
				query.ColumnSet.AddColumn("name");
				query.ColumnSet.AddColumn("statecode");
				query.Criteria.AddCondition("workflowid", ConditionOperator.Equal, command.WorkflowId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Workflow '{command.WorkflowId}' not found.");
				}

				var workflow = results.Entities[0];
				var currentState = workflow.GetAttributeValue<OptionSetValue>("statecode")?.Value;

				if (currentState == targetState)
				{
					output.WriteLine($"Workflow '{workflow.GetAttributeValue<string>("name")}' is already {(targetState == 1 ? "activated" : "deactivated")}.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				var stateName = targetState == 1 ? "activated" : "deactivated";
				output.Write($"Setting workflow '{workflow.GetAttributeValue<string>("name")}' to {stateName}...");

				if (!Guid.TryParse(command.WorkflowId, out var workflowId))
				{
					return CommandResult.Fail($"Invalid workflow ID '{command.WorkflowId}'. Must be a GUID.");
				}

				// Use SetStateRequest to change workflow state
				var setStateRequest = new SetStateRequest
				{
					EntityMoniker = new EntityReference("workflow", workflowId),
					State = new OptionSetValue(targetState),
					Status = new OptionSetValue(targetState == 1 ? 2 : 1), // 2=Activated, 1=Deactivated
				};

				await crm.ExecuteAsync(setStateRequest, cancellationToken);

				output.WriteLine($" Done ({stateName})", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error setting workflow state: {ex.Message}", ex);
			}
		}
	}
}
