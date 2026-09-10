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
		IWorkflowRepository workflowRepository)

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
				output.WriteLine("Warning: the file does not look like the definition of a flow (no properties.definition found). The flow is updated anyway, but it may not open in the designer.", ConsoleColor.Yellow);
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
					found = await workflowRepository.GetDefinitionByNameAsync(crm, searchedName, null);
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
				return CommandResult.Fail("Please provide the full name, or use the --id option to identify the one you need.");
			}

			var workflow = matches[0];

			if (workflow.category?.Value != (int)Workflow.Category.ModernFlow)
			{
				return CommandResult.Fail($"The workflow <{workflow.name}> is not a modern flow ({workflow.CategoryFormatted ?? "unknown category"}). Only modern flows can be updated from a json definition.");
			}

			if (string.Equals(workflow.clientdata, clientData, StringComparison.Ordinal))
			{
				output.WriteLine("The definition of the flow is already identical to the file, nothing to update.", ConsoleColor.Cyan);
				return CommandResult.Success();
			}

			var isActivated = workflow.statecode?.Value == (int)Workflow.State.Activated;

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
				output.WriteLine("The flow is currently activated, the new definition is effective immediately.", ConsoleColor.Cyan);
			}

			var result = CommandResult.Success();
			result["workflowid"] = workflow.Id;
			return result;
		}
	}
}
