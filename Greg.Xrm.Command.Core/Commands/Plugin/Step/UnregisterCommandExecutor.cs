using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;

namespace Greg.Xrm.Command.Commands.Plugin.Step
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
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while retrieving the plugin step: " + ex.Message);
			}






			output.Write("Looking for images...");
			var images = await sdkMessageProcessingStepImageRepository.GetByStepIdAsync(crm, step.Id);
			output.WriteLine($"Done ({images.Length} images found)", ConsoleColor.Green);

			try
			{
				var i = 0;
				foreach (var image in images)
				{
					i++;
					output.Write($"Deleting image {i}/{images.Length}...");
					await crm.DeleteAsync(image.EntityName, image.Id, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}


				output.Write("Deleting the plugin step...");
				await crm.DeleteAsync(step.EntityName, step.Id, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);



				if (step.sdkmessageprocessingstepsecureconfigid != null)
				{
					output.Write("Deleting the secure configuration...");
					var item = step.sdkmessageprocessingstepsecureconfigid;
					await crm.DeleteAsync(item.LogicalName, item.Id, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}
			}
			catch(Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail("An error occurred while deleting the plugin step: " + ex.Message);
			}

			return CommandResult.Success();
		}
	}
}
