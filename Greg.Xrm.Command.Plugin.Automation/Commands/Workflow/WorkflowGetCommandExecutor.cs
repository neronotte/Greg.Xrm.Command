using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Plugin.Automation.Commands.Workflow
{
	public class WorkflowGetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<WorkflowGetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WorkflowGetCommand command, CancellationToken cancellationToken)
		{
			if (command.WorkflowId == null && string.IsNullOrWhiteSpace(command.WorkflowName))
			{
				return CommandResult.Fail("Either workflow ID or name must be provided.");
			}

			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var query = new QueryExpression("workflow");
			query.ColumnSet = new ColumnSet("name", "clientdata");
			query.Criteria.AddCondition("category", ConditionOperator.Equal, 5); // Modern Flow

			if (command.WorkflowId.HasValue)
			{
				query.Criteria.AddCondition("workflowid", ConditionOperator.Equal, command.WorkflowId.Value);
			}
			else
			{
				query.Criteria.AddCondition("name", ConditionOperator.Equal, command.WorkflowName);
			}

			try
			{
				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (result.Entities.Count == 0)
				{
					return CommandResult.Fail("Workflow not found.");
				}

				var flow = result.Entities[0];
				var clientData = flow.GetAttributeValue<string>("clientdata");

				if (string.IsNullOrWhiteSpace(clientData))
				{
					return CommandResult.Fail("Workflow definition (clientdata) is empty.");
				}

				var path = Path.GetFullPath(command.OutputPath ?? "flow_definition.json");
				await File.WriteAllTextAsync(path, clientData, cancellationToken);

				output.WriteLine($"Flow definition saved to: {path}", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail("Error while retrieving workflow definition: " + ex.Message, ex);
			}
		}
	}
}
