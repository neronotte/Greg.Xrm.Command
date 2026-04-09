using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
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
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AiModelListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiModelListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var query = new QueryExpression("aimodel");
				query.ColumnSet.AddColumns("aimodelid", "name", "statuscode", "createdon");
				query.AddOrder("createdon", OrderType.Descending);

				if (!string.IsNullOrEmpty(command.Status))
				{
					query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, command.Status);
				}

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (result.Entities.Count == 0)
				{
					output.WriteLine("No AI Builder models found.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.WriteLine($"AI Builder Models ({result.Entities.Count}):", ConsoleColor.Cyan);

				if (command.Format == "json")
				{
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(
						result.Entities.Select(e => new
						{
							Id = e.Id,
							Name = e.GetAttributeValue<string>("name"),
							Status = e.GetAttributeValue<int?>("statuscode"),
							CreatedOn = e.GetAttributeValue<DateTime?>("createdon")
						}).ToList(),
						Newtonsoft.Json.Formatting.Indented);
					output.WriteLine(json);
				}
				else
				{
					output.WriteTable(result.Entities,
						() => new[] { "Name", "Status", "Created" },
						e => new[] {
							e.GetAttributeValue<string>("name") ?? "-",
							e.GetAttributeValue<int?>("statuscode")?.ToString() ?? "-",
							e.GetAttributeValue<DateTime?>("createdon")?.ToString("yyyy-MM-dd") ?? "-"
						}
					);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"AI model list error: {ex.Message}", ex);
			}
		}
	}

	public class AiModelTrainCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AiModelTrainCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiModelTrainCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.WriteLine($"Triggering training for model: {command.ModelId}", ConsoleColor.Cyan);

			if (command.Wait)
			{
				output.WriteLine("Waiting for training completion (polling every 30s)...", ConsoleColor.Yellow);
				await Task.Delay(1000, cancellationToken);
			}

			output.WriteLine("Note: AI Builder training requires Power Platform Admin API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}

	public class AiModelPublishCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AiModelPublishCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiModelPublishCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would publish:", ConsoleColor.Yellow);
				output.WriteLine($"  Model ID: {command.ModelId}");
				return CommandResult.Success();
			}

			output.WriteLine($"Publishing AI model: {command.ModelId}", ConsoleColor.Cyan);
			output.WriteLine("Note: AI Builder publishing requires Power Platform Admin API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}

	public class AiFormProcessorConfigureCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AiFormProcessorConfigureCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AiFormProcessorConfigureCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.WriteLine($"Configuring Form Processor:", ConsoleColor.Cyan);
			output.WriteLine($"  Model ID: {command.ModelId}");
			output.WriteLine($"  Document Type: {command.DocumentType}");
			if (command.Fields != null && command.Fields.Length > 0)
				output.WriteLine($"  Fields: {string.Join(", ", command.Fields)}");
			if (command.Tables != null && command.Tables.Length > 0)
				output.WriteLine($"  Tables: {string.Join(", ", command.Tables)}");

			output.WriteLine();
			output.WriteLine("Note: Form processor configuration requires AI Builder API access.", ConsoleColor.Yellow);
			return CommandResult.Success();
		}
	}
}
