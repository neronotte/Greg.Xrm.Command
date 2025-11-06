using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Workflows
{
	public class DeactivateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWorkflowRepository workflowRepository)

	: ICommandExecutor<DeactivateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeactivateCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			IReadOnlyCollection<Workflow> workflows;

			if (command.WorkflowId != Guid.Empty)
			{
				workflows = await workflowRepository.GetByIdsAsync(
					crm,
					[command.WorkflowId]);


				if (workflows.Count == 0)
				{
					return CommandResult.Fail($"No workflow found with ID '{command.WorkflowId}'.");
				}
			}
			else if (!string.IsNullOrWhiteSpace(command.WorkflowName))
			{
				workflows = await workflowRepository.GetByNameAsync(
					crm,
					command.WorkflowName);


				if (workflows.Count == 0)
				{
					return CommandResult.Fail($"No workflow found with name '{command.WorkflowName}'.");
				}
				if (workflows.Count > 1)
				{
					return CommandResult.Fail($"Multiple workflows found with name '{command.WorkflowName}'. Please use the --id option to specify the workflow to deactivate.");
				}
			}
			else
			{
				var solutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				}
				if (string.IsNullOrWhiteSpace(solutionName))
				{
					return CommandResult.Fail("No solution name provided and unable to determine the default solution of the current environment. Please provide a solution name using the --solution option.");
				}

				workflows = await workflowRepository.GetBySolutionAsync(
					crm,
					solutionName);
			}


			int successCount = 0;
			int failCount = 0;
			foreach (var workflow in workflows)
			{
				if (workflow.statuscode?.Value == (int)Workflow.Status.Draft)
				{
					output.WriteLine($"Workflow '{workflow.name}' (ID: {workflow.Id}) is already deactivated, nothing to do.", ConsoleColor.Cyan);
					successCount++;
					continue;
				}

				output.Write($"Deactivating workflow '{workflow.name}' (ID: {workflow.Id})... ");
				try
				{
					workflow.statecode = new OptionSetValue((int)Workflow.State.Draft);
					workflow.statuscode = new OptionSetValue((int)Workflow.Status.Draft);
					await workflow.SaveOrUpdateAsync(crm);

					output.WriteLine("Done", ConsoleColor.Green);
					successCount++;
				}
				catch(FaultException<OrganizationServiceFault> ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					output.WriteLine(ex.Dump());
					failCount++;
				}
			}

			if (failCount == 0)
			{
				return CommandResult.Success();
			}
			if (successCount == 0)
			{
				return CommandResult.Fail("No workflow was deactivated.");
			}

			return CommandResult.Fail($"{failCount}/{workflows.Count} workflow(s) failed to be deactivated.");
		}
	}
}
