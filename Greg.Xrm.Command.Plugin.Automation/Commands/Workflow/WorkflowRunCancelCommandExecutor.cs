using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Workflow
{
	public class WorkflowRunCancelCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<WorkflowRunCancelCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WorkflowRunCancelCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Get the workflow run
				var query = new QueryExpression("asyncoperation");
				query.ColumnSet.AddColumn("asyncoperationid");
				query.ColumnSet.AddColumn("name");
				query.ColumnSet.AddColumn("statecode");
				query.Criteria.AddCondition("asyncoperationid", ConditionOperator.Equal, command.RunId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Workflow run '{command.RunId}' not found.");
				}

				var run = results.Entities[0];
				var state = run.GetAttributeValue<OptionSetValue>("statecode")?.Value;

				// Only running workflows can be canceled
				if (state is not (10 or 20 or 21)) // Waiting, InProgress, Pausing
				{
					return CommandResult.Fail($"Cannot cancel workflow run in state {state}. Only running workflows can be canceled.");
				}

				output.Write($"Canceling workflow run {command.RunId} ({run.GetAttributeValue<string>("name")})...");

				// Update state to Canceled (32)
				run["statecode"] = new OptionSetValue(32);
				run["statuscode"] = new OptionSetValue(32);
				await crm.UpdateAsync(run, cancellationToken);

				output.WriteLine(" Done", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error canceling workflow: {ex.Message}", ex);
			}
		}
	}
}
