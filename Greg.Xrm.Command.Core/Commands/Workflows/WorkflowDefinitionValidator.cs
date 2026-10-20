using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Workflows
{
	/// <summary>
	/// Validates a definition in three layers before it may reach its target flow:
	/// 1. the stable base grammar is checked locally (at least one trigger and one
	///    action, runAfter dependencies must exist),
	/// 2. the connection references used by the definition must exist in the environment,
	/// 3. the definition is written to a short-lived probe flow which is activated once:
	///    the activation is the only supported way to run the template validation of the
	///    flow engine (the solution import does not validate the definition, and the api
	///    of the maker portal is explicitly unsupported). A trigger condition that can
	///    never be true is added to the probe, so it cannot fire while it is activated.
	/// The target flow itself is only touched after all three layers passed.
	/// </summary>
	public class WorkflowDefinitionValidator(
		IOutput output,
		IConnectionReferenceRepository connectionReferenceRepository)

	: IWorkflowDefinitionValidator
	{
		public async Task<CommandResult?> ValidateAsync(IOrganizationServiceAsync2 crm, string clientData, CancellationToken cancellationToken)
		{
			var structureErrors = WorkflowClientData.GetStructureErrors(clientData);
			if (structureErrors.Count > 0)
			{
				return CommandResult.Fail("The definition is not a valid flow definition: " + string.Join(" ", structureErrors));
			}

			cancellationToken.ThrowIfCancellationRequested();

			var referenceNames = WorkflowClientData.GetConnectionReferenceLogicalNames(clientData);
			if (referenceNames.Count > 0)
			{
				output.Write("Checking the connection references...");
				IReadOnlyCollection<string> existing;
				try
				{
					existing = await connectionReferenceRepository.GetExistingLogicalNamesAsync(crm, referenceNames);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"Unable to check the connection references: {ex.Message}", ex);
				}
				var missing = referenceNames.Where(n => !existing.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
				if (missing.Count > 0)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"The definition uses connection references that do not exist in this environment: {string.Join(", ", missing.Select(m => $"<{m}>"))}.");
				}
				output.WriteLine("Done", ConsoleColor.Green);
			}

			cancellationToken.ThrowIfCancellationRequested();

			return await RunActivationProbeAsync(crm, clientData);
		}


		private async Task<CommandResult?> RunActivationProbeAsync(IOrganizationServiceAsync2 crm, string clientData)
		{
			var probe = new Workflow
			{
				name = $"pacx validation probe {Guid.NewGuid():N}",
				category = new OptionSetValue((int)Workflow.Category.ModernFlow),
				type = new OptionSetValue((int)Workflow.Type.Definition),
				primaryentity = "none",
				clientdata = WorkflowClientData.AddTriggerMuzzle(clientData)
			};

			output.Write("Validating the definition against the flow engine...");
			try
			{
				await probe.SaveOrUpdateAsync(crm);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Unable to create the validation probe: {ex.Message}", ex);
			}

			try
			{
				try
				{
					// the activation is the actual validation: the flow engine parses the
					// template here and rejects it with a meaningful error when it is invalid
					probe.statecode = new OptionSetValue((int)Workflow.State.Activated);
					probe.statuscode = new OptionSetValue((int)Workflow.Status.Activated);
					await probe.SaveOrUpdateAsync(crm);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"The flow engine rejected the definition: {ex.Message}", ex);
				}

				try
				{
					probe.statecode = new OptionSetValue((int)Workflow.State.Draft);
					probe.statuscode = new OptionSetValue((int)Workflow.Status.Draft);
					await probe.SaveOrUpdateAsync(crm);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"Unable to deactivate the validation probe: {ex.Message}", ex);
				}

				output.WriteLine("Done", ConsoleColor.Green);
				return null;
			}
			finally
			{
				// whatever happens here must not hide the actual validation result
				try
				{
					await probe.DeleteAsync(crm);
				}
				catch (Exception ex)
				{
					output.WriteLine($"Unable to remove the validation probe flow <{probe.name}>: {ex.Message}", ConsoleColor.DarkYellow);
					output.WriteLine("Please remove it manually from the environment.", ConsoleColor.DarkYellow);
				}
			}
		}
	}
}
