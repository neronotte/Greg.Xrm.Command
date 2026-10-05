using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	public class UpdateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWorkflowRepository workflowRepository,
		IWorkflowDefinitionValidator workflowDefinitionValidator)

	: ICommandExecutor<UpdateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(UpdateCommand command, CancellationToken cancellationToken)
		{
			// the definition file is validated before connecting, to fail fast on the most likely mistakes
			var definition = await WorkflowDefinitionFile.ReadAsync(command.DefinitionFile, cancellationToken);
			if (definition.Error != null)
			{
				return CommandResult.Fail(definition.Error);
			}
			var clientData = definition.ClientData!;

			if (!definition.LooksLikeAFlowDefinition)
			{
				output.WriteLine("Warning: the file does not look like the definition of a flow (no properties.definition found). It is unlikely to pass the validation.", ConsoleColor.Yellow);
			}

			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var searchedName = command.Name.Trim();

			IReadOnlyList<Workflow> found;
			try
			{
				output.Write($"Retrieving workflow {(command.Id.HasValue ? command.Id.ToString() : searchedName)}...");

				if (command.Id.HasValue)
				{
					var byId = await workflowRepository.GetDefinitionByIdAsync(crm, command.Id.Value);
					found = byId == null ? [] : [byId];
				}
				else
				{
					found = await workflowRepository.GetDefinitionByNameAsync(crm, searchedName, command.SolutionName?.Trim());
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}

			if (found.Count == 0)
			{
				var searched = command.Id.HasValue ? $"id <{command.Id}>" : $"name <{searchedName}>";
				return CommandResult.Fail($"No workflow found with {searched}. You can use 'pacx workflow list' to see the available ones.");
			}

			// names typed into the maker portal can carry leading or trailing spaces,
			// so an exact match wins but a name that only differs by those is still found
			var matches = found;
			if (!command.Id.HasValue)
			{
				var exactMatches = found
					.Where(w => string.Equals(w.name?.Trim(), searchedName, StringComparison.OrdinalIgnoreCase))
					.ToList();

				if (exactMatches.Count > 0)
				{
					matches = exactMatches;
				}
			}

			if (matches.Count > 1)
			{
				output.WriteLine();
				output.WriteLine($"More than one workflow matches <{searchedName}>:", ConsoleColor.Yellow);
				foreach (var candidate in matches)
				{
					output.WriteLine($"  {candidate.name}", ConsoleColor.Yellow);
				}
				return CommandResult.Fail("Please provide the full name, or use the --solution or --id option to identify the one you need.");
			}

			var workflow = matches[0];

			if (workflow.category?.Value != (int)Workflow.Category.ModernFlow)
			{
				return CommandResult.Fail($"The workflow <{workflow.name}> is not a modern flow ({workflow.CategoryFormatted ?? "unknown category"}). Only modern flows can be updated from a json definition.");
			}

			if (string.Equals(workflow.clientdata, clientData, StringComparison.Ordinal))
			{
				output.WriteLine("The definition of the flow is already identical to the file, nothing to update.", ConsoleColor.Cyan);
				var noChange = CommandResult.Success();
				noChange["workflowid"] = workflow.Id;
				return noChange;
			}

			var isActivated = workflow.statecode?.Value == (int)Workflow.State.Activated;

			// the definition is validated before the target flow is touched, so a
			// rejected definition leaves the existing flow exactly as it was
			var validationError = await workflowDefinitionValidator.ValidateAsync(crm, clientData, cancellationToken);
			if (validationError != null)
			{
				return validationError;
			}

			output.Write($"Updating flow <{workflow.name}>...");
			try
			{
				workflow.clientdata = clientData;
				await workflow.SaveOrUpdateAsync(crm);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to update the flow: {ex.Message}", ex);
			}
			output.WriteLine("Done", ConsoleColor.Green);

			if (isActivated)
			{
				// updating the clientdata of an activated flow becomes effective right away,
				// the state is verified instead of assumed to avoid a silently stopped flow
				var refreshed = await workflowRepository.GetDefinitionByIdAsync(crm, workflow.Id);
				if (refreshed == null)
				{
					output.WriteLine("Warning: unable to verify the state of the flow after the update. Please check that it is still activated.", ConsoleColor.Yellow);
				}
				else if (refreshed.statecode?.Value != (int)Workflow.State.Activated)
				{
					output.WriteLine("Warning: the flow was activated before the update, but it is not anymore. Please reactivate it with 'pacx workflow activate'.", ConsoleColor.Yellow);
				}
				else
				{
					output.WriteLine("The flow is currently activated, the new definition is effective immediately.", ConsoleColor.Cyan);
				}
			}

			var result = CommandResult.Success();
			result["workflowid"] = workflow.Id;
			return result;
		}
	}
}
