using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Workflows
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWorkflowRepository workflowRepository)

	: ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{


			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
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
				return CommandResult.Fail(ex.Message, ex);
			}

			if (workflowList.Count == 0)
			{
				output.WriteLine("No workflow found matching the specified criteria.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}


			output.WriteLine();
			output.WriteTable(workflowList,
				() => ["Id", "Name", "Category", "State", "Status"],
				w => [w.Id.ToString(), w.name ?? string.Empty, w.CategoryFormatted ?? string.Empty, w.StateCodeFormatted ?? string.Empty, w.StatusCodeFormatted ?? string.Empty],
				(col, row) => {

					if (col == 1) return ConsoleColor.White;
					if (col == 0 || col == 2) return ConsoleColor.DarkGray;
					return row.statecode?.Value == (int)Workflow.State.Activated ? ConsoleColor.Green : ConsoleColor.DarkGray;
				});

			return CommandResult.Success();
		}
	}
}
