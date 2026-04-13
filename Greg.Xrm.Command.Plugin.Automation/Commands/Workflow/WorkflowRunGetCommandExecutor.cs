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
	public class WorkflowRunGetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<WorkflowRunGetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WorkflowRunGetCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Query workflow run
				var query = new QueryExpression("asyncoperation");
				query.ColumnSet.AddColumn("asyncoperationid");
				query.ColumnSet.AddColumn("name");
				query.ColumnSet.AddColumn("statecode");
				query.ColumnSet.AddColumn("statuscode");
				query.ColumnSet.AddColumn("startedon");
				query.ColumnSet.AddColumn("completedon");
				query.ColumnSet.AddColumn("ownerid");
				query.ColumnSet.AddColumn("regardingobjectid");
				query.Criteria.AddCondition("asyncoperationid", ConditionOperator.Equal, command.RunId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Workflow run '{command.RunId}' not found.");
				}

				var run = results.Entities[0];

				if (command.Format == "json")
				{
					var json = new
					{
						Id = run.Id,
						Name = run.GetAttributeValue<string>("name"),
						State = run.GetAttributeValue<OptionSetValue>("statecode")?.Value,
						Status = run.GetAttributeValue<OptionSetValue>("statuscode")?.Value,
						StartedOn = run.GetAttributeValue<DateTime?>("startedon"),
						CompletedOn = run.GetAttributeValue<DateTime?>("completedon"),
					};
					output.WriteLine(System.Text.Json.JsonSerializer.Serialize(json, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
				}
				else
				{
					var stateText = run.GetAttributeValue<OptionSetValue>("statecode")?.Value switch
					{
						0 => "Ready",
						10 => "Waiting",
						20 => "InProgress",
						21 => "Pausing",
						30 => "Succeeded",
						31 => "Failed",
						32 => "Canceled",
						_ => "Unknown",
					};

					output.WriteLine($"Workflow Run: {run.GetAttributeValue<string>("name")}", ConsoleColor.Cyan);
					output.WriteLine($"  ID: {run.Id}");
					output.WriteLine($"  State: {stateText}");
					output.WriteLine($"  Started: {run.GetAttributeValue<DateTime?>("startedon"):yyyy-MM-dd HH:mm:ss}");
					output.WriteLine($"  Completed: {run.GetAttributeValue<DateTime?>("completedon"):yyyy-MM-dd HH:mm:ss}");

					if (command.IncludeActions)
					{
						output.WriteLine();
						output.WriteLine("Note: Action outputs are stored in the workflow execution context.", ConsoleColor.Yellow);
						output.WriteLine("Use the Power Automate portal to inspect individual action outputs.");
					}
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error getting workflow run: {ex.Message}", ex);
			}
		}
	}
}
