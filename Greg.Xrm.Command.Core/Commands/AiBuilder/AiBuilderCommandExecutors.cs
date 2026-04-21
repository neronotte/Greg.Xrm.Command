using Greg.Xrm.Command.Services.AiBuilder;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.AiBuilder
{
	public class AiModelListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IAiBuilderApiClientFactory aiBuilderApiClientFactory) : ICommandExecutor<AiModelListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiModelListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var client = await aiBuilderApiClientFactory.CreateAsync(cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var models = (await client.ListModelsAsync(cancellationToken)).ToList();

				if (models.Count == 0)
				{
					output.WriteLine("No AI Builder models found.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.WriteLine($"AI Builder Models ({models.Count}):", ConsoleColor.Cyan);

				if (command.Format == "json")
				{
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(models, Newtonsoft.Json.Formatting.Indented);
					output.WriteLine(json);
				}
				else
				{
					output.WriteTable(models,
						() => new[] { "Name", "Status", "Created" },
						m => new[] {
							m.Name,
							m.Status,
							m.CreatedOn?.ToString("yyyy-MM-dd") ?? "-"
						}
					);
				}

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"AI model list error: {ex.Message}", ex);
			}
		}
	}

	public class AiModelTrainCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IAiBuilderApiClientFactory aiBuilderApiClientFactory) : ICommandExecutor<AiModelTrainCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiModelTrainCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to AI Builder...");
			var client = await aiBuilderApiClientFactory.CreateAsync(cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.WriteLine($"Triggering training for model: {command.ModelId}", ConsoleColor.Cyan);
				await client.TrainModelAsync(command.ModelId, command.Wait, cancellationToken);

				if (command.Wait)
				{
					output.WriteLine("Model training triggered successfully and completed!", ConsoleColor.Green);
				}
				else
				{
					output.WriteLine("Model training triggered successfully!", ConsoleColor.Green);
				}
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"AI model train error: {ex.Message}", ex);
			}
		}
	}

	public class AiModelPublishCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IAiBuilderApiClientFactory aiBuilderApiClientFactory) : ICommandExecutor<AiModelPublishCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiModelPublishCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to AI Builder...");
			var client = await aiBuilderApiClientFactory.CreateAsync(cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would publish:", ConsoleColor.Yellow);
				output.WriteLine($"  Model ID: {command.ModelId}");
				return CommandResult.Success();
			}

			try
			{
				output.WriteLine($"Publishing AI model: {command.ModelId}", ConsoleColor.Cyan);
				await client.PublishModelAsync(command.ModelId, cancellationToken);
				output.WriteLine("Model published successfully!", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"AI model publish error: {ex.Message}", ex);
			}
		}
	}

	public class AiFormProcessorConfigureCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IAiBuilderApiClientFactory aiBuilderApiClientFactory) : ICommandExecutor<AiFormProcessorConfigureCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiFormProcessorConfigureCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to AI Builder...");
			var client = await aiBuilderApiClientFactory.CreateAsync(cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.WriteLine($"Configuring Form Processor:", ConsoleColor.Cyan);
				output.WriteLine($"  Model ID: {command.ModelId}");
				output.WriteLine($"  Document Type: {command.DocumentType}");

				await client.ConfigureFormProcessorAsync(
					command.ModelId,
					command.DocumentType,
					command.Fields,
					command.Tables,
					cancellationToken);

				output.WriteLine("Form processor configured successfully!", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Form processor configuration error: {ex.Message}", ex);
			}
		}
	}
}
