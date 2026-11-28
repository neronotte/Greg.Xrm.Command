using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Workflows
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWorkflowRepository workflowRepository,
		IPowerAutomateFlowClient flowClient)

	: ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{


			output.Write($"Connecting to the current dataverse environment...");
			IOrganizationServiceAsync2 crm;
			try
			{
				crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			}
			catch (Exception ex) when (command.Live && !cancellationToken.IsCancellationRequested)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to connect to Dataverse: {ex.Message}", 2, ex);
			}
			output.WriteLine("Done", ConsoleColor.Green);


			var solutionName = command.SolutionName?.Trim();
			if (!"*".Equals(solutionName, StringComparison.OrdinalIgnoreCase))
			{
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				}
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					return CommandResult.Fail("No solution name provided and unable to determine the default solution of the current environment. Please provide a solution name using the --solution option.");
				}
			}
			else
			{
				solutionName = null;
			}

			IReadOnlyList<Workflow> workflowList;
			try
			{
				output.Write($"Retrieving workflows...");
				workflowList = await workflowRepository.SearchByNameAndSolutionAndCategoryAsync(crm, command.SearchQuery.Trim(), solutionName, command.Category);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return command.Live ? CommandResult.Fail(ex.Message, 2, ex) : CommandResult.Fail(ex.Message, ex);
			}

			if (workflowList.Count == 0)
			{
				output.WriteLine("No workflow found matching the specified criteria.", ConsoleColor.Yellow);
				if (!command.Live)
					return CommandResult.Success();
			}

			if (command.Live)
				return await WriteLiveResultsAsync(crm, workflowList, command.FailOnMismatch, cancellationToken);

			output.WriteLine();
			output.WriteTable(workflowList,
				() => ["Id", "Name", "Category", "State", "Status"],
				WorkflowColumns, WorkflowColumnColor);

			return CommandResult.Success();
		}

		private async Task<CommandResult> WriteLiveResultsAsync(IOrganizationServiceAsync2 crm, IReadOnlyList<Workflow> workflows,
			bool failOnMismatch, CancellationToken cancellationToken)
		{
			IReadOnlyList<PowerAutomateFlow> flows = [];
			if (workflows.Any(WorkflowLiveState.IsCloudFlow))
			{
				output.Write("Retrieving live Power Automate flows...");
				try
				{
					var response = (RetrieveCurrentOrganizationResponse)await crm.ExecuteAsync(new RetrieveCurrentOrganizationRequest(), cancellationToken);
					var environmentId = response.Detail?.EnvironmentId;
					if (string.IsNullOrWhiteSpace(environmentId))
						throw new InvalidOperationException("Dataverse did not return the Power Platform environment ID.");

					flows = await flowClient.ListAsync(environmentId, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}
				catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"Unable to read live Power Automate state: {ex.Message}", 2, ex);
				}
			}

			var rows = WorkflowLiveState.Compare(workflows, flows);
			if (rows.Count > 0)
			{
				output.WriteLine();
				output.WriteTable(rows,
					() => ["Id", "Name", "Category", "State", "Status", "Dataverse", "Power Automate", "Check", "Reason"],
					row => [.. WorkflowColumns(row.Workflow), row.Dataverse, row.PowerAutomate, row.Check, row.Reason],
					(col, row) => row.Check switch
					{
						"MISMATCH" => ConsoleColor.Red,
						"UNKNOWN" => ConsoleColor.Yellow,
						"OK" when col == 7 => ConsoleColor.Green,
						_ => col < 5 ? WorkflowColumnColor(col, row.Workflow) : ConsoleColor.DarkGray
					});
			}

			var ok = rows.Count(row => row.Check == "OK");
			var mismatches = rows.Count(row => row.Check == "MISMATCH");
			var unknown = rows.Count(row => row.Check == "UNKNOWN");
			output.WriteLine($"{ok + mismatches + unknown} flows: {ok} OK, {mismatches} MISMATCH, {unknown} UNKNOWN");
			return failOnMismatch && (mismatches > 0 || unknown > 0)
				? CommandResult.Fail("One or more cloud flows did not pass the live state check.", 1)
				: CommandResult.Success();
		}

		private static string[] WorkflowColumns(Workflow workflow) =>
			[workflow.Id.ToString(), workflow.name ?? string.Empty, workflow.CategoryFormatted ?? string.Empty,
				workflow.StateCodeFormatted ?? string.Empty, workflow.StatusCodeFormatted ?? string.Empty];

		private static ConsoleColor? WorkflowColumnColor(int col, Workflow workflow)
		{
			if (col == 1) return ConsoleColor.White;
			if (col == 0 || col == 2) return ConsoleColor.DarkGray;
			return workflow.statecode?.Value == (int)Workflow.State.Activated ? ConsoleColor.Green : ConsoleColor.DarkGray;
		}
	}
}
