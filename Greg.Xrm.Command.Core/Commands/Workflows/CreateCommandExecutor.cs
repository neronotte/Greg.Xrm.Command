using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	public class CreateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISolutionRepository solutionRepository,
		IWorkflowRepository workflowRepository,
		IWorkflowDefinitionValidator workflowDefinitionValidator)

	: ICommandExecutor<CreateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
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

			var solutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(solutionName))
			{
				solutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
			}
			if (string.IsNullOrWhiteSpace(solutionName))
			{
				return CommandResult.Fail("No solution name provided and no current default solution found in the settings. Please provide a solution name using the --solution option.");
			}

			output.Write($"Validating solution <{solutionName}>...");
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, solutionName);
			if (solution == null)
			{
				output.WriteLine("Not found", ConsoleColor.Red);
				return CommandResult.Fail($"Solution <{solutionName}> not found.");
			}
			if (solution.ismanaged)
			{
				output.WriteLine("Managed", ConsoleColor.Red);
				return CommandResult.Fail($"Solution <{solutionName}> is managed. Cannot add components to a managed solution.");
			}
			output.WriteLine("Done", ConsoleColor.Green);

			var name = command.Name.Trim();

			// names typed into the maker portal can carry leading or trailing spaces,
			// so the duplicate check tolerates them the same way get and update do
			var similar = await workflowRepository.SearchByNameAndSolutionAndCategoryAsync(crm, name, solutionUniqueName: null, category: null);
			var duplicate = similar.FirstOrDefault(w => string.Equals(w.name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
			if (duplicate != null)
			{
				return CommandResult.Fail($"A workflow named <{duplicate.name}> already exists in this environment. Please choose a different name, or delete the existing one first.");
			}

			// the definition is validated before the flow exists, so no malformed
			// definition can ever reach a flow that people actually see
			var validationError = await workflowDefinitionValidator.ValidateAsync(crm, clientData, cancellationToken);
			if (validationError != null)
			{
				return validationError;
			}

			var workflow = new Workflow
			{
				name = name,
				category = new OptionSetValue((int)Workflow.Category.ModernFlow),
				type = new OptionSetValue((int)Workflow.Type.Definition),
				primaryentity = "none",
				clientdata = clientData
			};

			output.Write($"Creating flow <{name}>...");
			try
			{
				await workflow.SaveOrUpdateAsync(crm);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to create the flow: {ex.Message}", ex);
			}
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write($"Adding the flow to solution <{solutionName}>...");
			try
			{
				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = solutionName,
					ComponentType = (int)ComponentType.Workflow,
					ComponentId = workflow.Id
				};
				await crm.ExecuteAsync(request, cancellationToken);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"The flow has been created (id <{workflow.Id}>) but could not be added to solution <{solutionName}>: {ex.Message}", ex);
			}
			output.WriteLine("Done", ConsoleColor.Green);

			output.WriteLine();
			output.Write("The flow has been created in draft state, with id ")
				.Write(workflow.Id.ToString(), ConsoleColor.Yellow)
				.WriteLine(".");
			output.WriteLine("Open it in the flow designer to verify that the connection references resolve, then activate it with 'pacx workflow activate'.");

			var result = CommandResult.Success();
			result["workflowid"] = workflow.Id;
			return result;
		}
	}
}
