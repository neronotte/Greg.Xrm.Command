using System.ServiceModel;
using Greg.Xrm.Command.Commands.Data.RecordPayload;
using Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Update
{
	public class UpdateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
	) : ICommandExecutor<UpdateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(UpdateCommand command, CancellationToken cancellationToken)
		{
			// 1. Parse input
			Dictionary<string, object?> rawPayload;
			try
			{
				rawPayload = ParsePayload(command);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				return CommandResult.Fail($"Error parsing input payload: {ex.Message}", ex);
			}

			// 2. Connect
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			// 3. Retrieve entity metadata
			output.Write($"Retrieving metadata for table '{command.Table}'...");
			EntityMetadata entityMetadata;
			try
			{
				var request = new RetrieveEntityRequest
				{
					LogicalName = command.Table!,
					EntityFilters = EntityFilters.Attributes
				};
				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request, cancellationToken);
				entityMetadata = response.EntityMetadata;
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Table '{command.Table}' not found or inaccessible: {ex.Message}", ex);
			}

			// 4. Process payload
			output.Write("Processing field values...");
			var processor = new RecordPayloadProcessor();
			RecordPayloadProcessor.ProcessResult processResult;
			try
			{
				processResult = await processor.ProcessAsync(
					rawPayload,
					entityMetadata,
					validatingForCreate: false,
					crm,
					cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error processing payload: {ex.Message}", ex);
			}

			// 5. Check errors
			if (processResult.Errors.Count > 0)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				foreach (var error in processResult.Errors)
				{
					output.WriteLine($"Error: {error}", ConsoleColor.Red);
				}
				return CommandResult.Fail($"Payload validation failed with {processResult.Errors.Count} error(s).");
			}

			output.WriteLine("Done", ConsoleColor.Green);

			// 6. Emit warnings
			foreach (var warning in processResult.Warnings)
			{
				output.WriteLine($"Warning: {warning}", ConsoleColor.Yellow);
			}

			// 7. Always set the record ID
			processResult.Entity.Id = command.Id;

			// 8. Dry-run
			if (command.DryRun)
			{
				output.WriteLine();
				output.WriteLine("Dry-run mode: the following fields would be updated:", ConsoleColor.Cyan);
				PrintEntityFields(processResult.Entity);
				return CommandResult.Success();
			}

			// 9. Update the record
			output.Write($"Updating record {command.Id}...");
			try
			{
				await crm.UpdateAsync(processResult.Entity, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error updating record: {ex.Message}", ex);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Unexpected error updating record: {ex.Message}", ex);
			}

			// 10. Output result
			output.WriteLine();
			output.Write("Record updated successfully.  Table: ").WriteLine(command.Table, ConsoleColor.Cyan);

			// 11. Return fields if requested
			if (!string.IsNullOrWhiteSpace(command.Return))
			{
				var columns = command.Return
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				output.Write("Retrieving returned fields...");
				try
				{
					var retrieved = await crm.RetrieveAsync(
						entityMetadata.LogicalName,
						command.Id,
						new ColumnSet(columns),
						cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
					output.WriteLine();
					PrintEntityFields(retrieved);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					output.WriteLine("FAILED", ConsoleColor.Red);
					output.WriteLine($"Warning: Could not retrieve record fields: {ex.Message}", ConsoleColor.Yellow);
				}
			}

			var result = CommandResult.Success();
			result["Id"] = command.Id;
			return result;
		}

		private static Dictionary<string, object?> ParsePayload(UpdateCommand command)
		{
			if (!string.IsNullOrWhiteSpace(command.Plain))
			{
				var raw = PlainPayloadParser.Parse(command.Plain);
				return raw.ToDictionary(
					kvp => kvp.Key,
					kvp => (object?)kvp.Value,
					StringComparer.OrdinalIgnoreCase);
			}

			if (!string.IsNullOrWhiteSpace(command.Json))
			{
				return JsonPayloadParser.ParseInline(command.Json);
			}

			if (!string.IsNullOrWhiteSpace(command.File))
			{
				return JsonPayloadParser.ParseFile(command.File);
			}

			throw new InvalidOperationException("No payload source specified.");
		}

		private void PrintEntityFields(Entity entity)
		{
			var fields = entity.Attributes
				.Select(a => new { Name = a.Key, Value = FormatValue(a.Value) })
				.ToList();

			if (fields.Count == 0)
			{
				output.WriteLine("(no fields)");
				return;
			}

			output.WriteTable(
				fields,
				() => ["Field", "Value"],
				row => [row.Name, row.Value]);
		}

		private static string FormatValue(object? value)
		{
			return value switch
			{
				null => "(null)",
				EntityReference er => $"{er.LogicalName}({er.Id})",
				Microsoft.Xrm.Sdk.OptionSetValue osv => osv.Value.ToString(),
				Microsoft.Xrm.Sdk.OptionSetValueCollection values => string.Join(", ", values.Select(item => item.Value)),
				Microsoft.Xrm.Sdk.Money money => money.Value.ToString("F2"),
				_ => value.ToString() ?? "(null)"
			};
		}
	}
}
