using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Plugin.Step
{
	public class DisableCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPluginTypeRepository pluginTypeRepository,
		ISdkMessageProcessingStepRepository sdkMessageProcessingStepRepository
	) : ICommandExecutor<DisableCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DisableCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);



			output.Write("Retrieving the plugin step...");
			SdkMessageProcessingStep? step;

			try
			{
				if (command.StepId != null)
				{
					step = await sdkMessageProcessingStepRepository.GetByIdAsync(crm, command.StepId.Value);
				}
				else
				{
					var pluginType = await pluginTypeRepository.FuzzySearchAsync(crm, command.PluginTypeName, cancellationToken);
					if (pluginType.Length == 0)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail("Invalid plugin type: " + command.PluginTypeName);
					}
					if (pluginType.Length > 1)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail("Ambiguous plugin type reference: " + command.PluginTypeName);
					}


					var steps = await sdkMessageProcessingStepRepository.GetByKeyAsync(crm, pluginType[0], command.MessageName, command.PrimaryEntityName, command.Stage!.Value);
					if (steps.Length > 1)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail("Ambiguous reference. Please specify the step ID.");
					}

					step = steps[0];
				}
				if (step == null)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail("Unable to retrieve a step with the given key.");
				}
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while retrieving the plugin step: " + ex.Message);
			}






			

			try
			{
				if (step.statecode?.Value == 1)
				{
					output.WriteLine("The step is already disabled.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.Write("Disabling step...");

				step.statecode = new OptionSetValue(1); // Disabled
				step.statuscode = new OptionSetValue(2); // Disabled

				await step.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while disabling the plugin step: " + ex.Message);
			}

			return CommandResult.Success();
		}
	}

	public class EnableCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPluginTypeRepository pluginTypeRepository,
		ISdkMessageProcessingStepRepository sdkMessageProcessingStepRepository
	) : ICommandExecutor<EnableCommand>
	{
		public async Task<CommandResult> ExecuteAsync(EnableCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);



			output.Write("Retrieving the plugin step...");
			SdkMessageProcessingStep? step;

			try
			{
				if (command.StepId != null)
				{
					step = await sdkMessageProcessingStepRepository.GetByIdAsync(crm, command.StepId.Value);
				}
				else
				{
					var pluginType = await pluginTypeRepository.FuzzySearchAsync(crm, command.PluginTypeName, cancellationToken);
					if (pluginType.Length == 0)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail("Invalid plugin type: " + command.PluginTypeName);
					}
					if (pluginType.Length > 1)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail("Ambiguous plugin type reference: " + command.PluginTypeName);
					}


					var steps = await sdkMessageProcessingStepRepository.GetByKeyAsync(crm, pluginType[0], command.MessageName, command.PrimaryEntityName, command.Stage!.Value);
					if (steps.Length > 1)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail("Ambiguous reference. Please specify the step ID.");
					}

					step = steps[0];
				}
				if (step == null)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail("Unable to retrieve a step with the given key.");
				}
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while retrieving the plugin step: " + ex.Message);
			}








			try
			{
				if (step.statecode?.Value == 0)
				{
					output.WriteLine("The step is already enabled.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.Write("Enabling step...");

				step.statecode = new OptionSetValue(0); // Enabled
				step.statuscode = new OptionSetValue(1); // Enabled

				await step.SaveOrUpdateAsync(crm);
				output.WriteLine("Done", ConsoleColor.Green);

			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while enabling the plugin step: " + ex.Message);
			}

			return CommandResult.Success();
		}
	}
}
