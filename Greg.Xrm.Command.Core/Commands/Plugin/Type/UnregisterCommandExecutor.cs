using Greg.Xrm.Command.Commands.Plugin;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;

namespace Greg.Xrm.Command.Commands.Plugin.Type
{
	public class UnregisterCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPluginTypeRepository pluginTypeRepository,
		ISdkMessageProcessingStepRepository sdkMessageProcessingStepRepository,
		ISdkMessageProcessingStepImageRepository sdkMessageProcessingStepImageRepository
	) : ICommandExecutor<UnregisterCommand>
	{
		public async Task<CommandResult> ExecuteAsync(UnregisterCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			// Resolve the plugin type
			output.Write("Retrieving the plugin type...");
			PluginType? pluginType;

			try
			{
				if (command.TypeId != null)
				{
					pluginType = await pluginTypeRepository.GetByIdAsync(crm, command.TypeId.Value, cancellationToken);
				}
				else
				{
					var matches = await pluginTypeRepository.FuzzySearchAsync(crm, command.PluginTypeName!, cancellationToken);
					if (matches.Length == 0)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail($"No plugin type found matching '{command.PluginTypeName}'.");
					}
					if (matches.Length > 1)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						output.WriteLine($"Ambiguous plugin type reference: {matches.Length} types match '{command.PluginTypeName}'.");
						output.WriteLine("Please use --id to specify the exact plugin type, or use a more specific --name.");
						output.WriteTable(matches,
							rowHeaders: () => ["Id", "Name"],
							rowData: pt => [pt.Id.ToString(), pt.name ?? "(unknown)"]);
						return CommandResult.Fail("Ambiguous plugin type name.");
					}
					pluginType = matches[0];
				}

				if (pluginType == null)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail("Plugin type not found.");
				}

				output.WriteLine($"Done ({pluginType.name})", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while retrieving the plugin type: " + ex.Message);
			}


			// Check for registered steps
			output.Write("Checking for registered steps...");
			SdkMessageProcessingStep[] steps;

			try
			{
				steps = await sdkMessageProcessingStepRepository.GetByPluginTypeIdAsync(crm, pluginType.Id, false, cancellationToken);
				output.WriteLine($"Done ({steps.Length} step(s) found)", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while checking for registered steps: " + ex.Message);
			}


			if (steps.Length > 0 && !command.Force)
			{
				output.WriteLine();
				output.WriteLine($"Plugin type '{pluginType.name}' has {steps.Length} registered step(s):", ConsoleColor.Yellow);

				output.WriteTable(steps,
					rowHeaders: () => ["Step Name", "Message", "Table", "Stage"],
					rowData: s =>
					[
						s.name ?? "(unknown)",
						s.messagename,
						string.IsNullOrEmpty(s.primaryobjecttypecode) ? "(Global)" : s.primaryobjecttypecode,
						PluginStepInfo.GetStageDisplayName(s.stage?.Value)
					]);

				output.WriteLine();
				return CommandResult.Fail(
					$"Cannot unregister plugin type '{pluginType.name}' because it has {steps.Length} registered step(s). " +
					"Use --force to automatically delete all steps before removing the plugin type.");
			}


			// Delete steps (and their images/secure configs) when --force is set
			if (steps.Length > 0)
			{
				try
				{
					for (var i = 0; i < steps.Length; i++)
					{
						var step = steps[i];

						var images = await sdkMessageProcessingStepImageRepository.GetByStepIdAsync(crm, step.Id);

						for (var j = 0; j < images.Length; j++)
						{
							output.Write($"Deleting image {j + 1}/{images.Length} of step {i + 1}/{steps.Length}...");
							await crm.DeleteAsync(images[j].EntityName, images[j].Id, cancellationToken);
							output.WriteLine("Done", ConsoleColor.Green);
						}

						output.Write($"Deleting step {i + 1}/{steps.Length} ({step.name})...");
						await crm.DeleteAsync(step.EntityName, step.Id, cancellationToken);
						output.WriteLine("Done", ConsoleColor.Green);

						if (step.sdkmessageprocessingstepsecureconfigid != null)
						{
							output.Write($"Deleting secure configuration for step {i + 1}/{steps.Length}...");
							var secureConfig = step.sdkmessageprocessingstepsecureconfigid;
							await crm.DeleteAsync(secureConfig.LogicalName, secureConfig.Id, cancellationToken);
							output.WriteLine("Done", ConsoleColor.Green);
						}
					}
				}
				catch (Exception ex)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail("An error occurred while deleting plugin steps: " + ex.Message);
				}
			}


			// Delete the plugin type
			try
			{
				output.Write($"Deleting plugin type '{pluginType.name}'...");
				await crm.DeleteAsync(pluginType.EntityName, pluginType.Id, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while deleting the plugin type: " + ex.Message);
			}

			return CommandResult.Success();
		}
	}
}
