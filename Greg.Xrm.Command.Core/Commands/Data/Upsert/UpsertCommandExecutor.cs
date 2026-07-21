using System.ServiceModel;
using Greg.Xrm.Command.Commands.Data.RecordPayload;
using Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.Upsert
{
	public class UpsertCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
	) : ICommandExecutor<UpsertCommand>
	{
		public async Task<CommandResult> ExecuteAsync(UpsertCommand command, CancellationToken cancellationToken)
		{
			// 1. Parse key
			Dictionary<string, object?> rawKey;
			try
			{
				var parsed = PlainPayloadParser.Parse(command.Key!);
				rawKey = parsed.ToDictionary(
					kvp => kvp.Key,
					kvp => (object?)kvp.Value,
					StringComparer.OrdinalIgnoreCase);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				return CommandResult.Fail($"Error parsing --key: {ex.Message}", ex);
			}

			if (rawKey.Count == 0)
			{
				return CommandResult.Fail("The --key option must contain at least one field=value pair.");
			}

			// 2. Parse payload
			Dictionary<string, object?> rawPayload;
			try
			{
				rawPayload = ParsePayload(command);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				return CommandResult.Fail($"Error parsing input payload: {ex.Message}", ex);
			}

			// 3. Connect
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			// 4. Retrieve entity metadata
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

			// 5. Process payload
			output.Write("Processing field values...");
			var processor = new RecordPayloadProcessor();
			RecordPayloadProcessor.ProcessResult payloadResult;
			try
			{
				payloadResult = await processor.ProcessAsync(
					rawPayload,
					entityMetadata,
					validatingForCreate: true,
					crm,
					cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error processing payload: {ex.Message}", ex);
			}

			// 6. Process key fields (to get typed values for KeyAttributes)
			RecordPayloadProcessor.ProcessResult keyResult;
			try
			{
				keyResult = await processor.ProcessAsync(
					rawKey,
					entityMetadata,
					validatingForCreate: false,
					crm,
					cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error processing key fields: {ex.Message}", ex);
			}

			// 7. Check errors
			var allErrors = payloadResult.Errors.Concat(keyResult.Errors).ToList();
			if (allErrors.Count > 0)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				foreach (var error in allErrors)
				{
					output.WriteLine($"Error: {error}", ConsoleColor.Red);
				}
				return CommandResult.Fail($"Payload validation failed with {allErrors.Count} error(s).");
			}

			output.WriteLine("Done", ConsoleColor.Green);

			// 8. Emit warnings
			foreach (var warning in payloadResult.Warnings.Concat(keyResult.Warnings))
			{
				output.WriteLine($"Warning: {warning}", ConsoleColor.Yellow);
			}

			// 9. Build the upsert entity
			var entity = payloadResult.Entity;
			foreach (var kvp in keyResult.Entity.Attributes)
			{
				entity.KeyAttributes[kvp.Key] = kvp.Value;
			}

			// 10. Dry-run
			if (command.DryRun)
			{
				output.WriteLine();
				output.WriteLine("Dry-run mode: the following fields would be set:", ConsoleColor.Cyan);
				output.WriteLine("Key attributes (used for record lookup):", ConsoleColor.Cyan);
				PrintKeyAttributes(entity.KeyAttributes);
				output.WriteLine("Payload attributes:", ConsoleColor.Cyan);
				PrintEntityFields(entity);
				return CommandResult.Success();
			}

			// 11. Execute the upsert
			output.Write($"Upserting record on table '{command.Table}'...");
			UpsertResponse upsertResponse;
			try
			{
				var upsertRequest = new UpsertRequest { Target = entity };
				upsertResponse = (UpsertResponse)await crm.ExecuteAsync(upsertRequest, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Error upserting record: {ex.Message}", ex);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				output.WriteLine("FAILED", ConsoleColor.Red);
				return CommandResult.Fail($"Unexpected error upserting record: {ex.Message}", ex);
			}

			// 12. Output result
			var recordId = upsertResponse.Target.Id;
			output.WriteLine();
			if (upsertResponse.RecordCreated)
			{
				output.Write("Record created successfully.  Table: ").WriteLine(command.Table, ConsoleColor.Cyan);
			}
			else
			{
				output.Write("Record updated successfully.  Table: ").WriteLine(command.Table, ConsoleColor.Cyan);
			}

			// 13. Return fields if requested
			if (!string.IsNullOrWhiteSpace(command.Return))
			{
				var columns = command.Return
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				output.Write("Retrieving returned fields...");
				try
				{
					var retrieved = await crm.RetrieveAsync(
						entityMetadata.LogicalName,
						recordId,
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
			result["Id"] = recordId;
			result["RecordCreated"] = upsertResponse.RecordCreated;
			return result;
		}

		private static Dictionary<string, object?> ParsePayload(UpsertCommand command)
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

		private void PrintKeyAttributes(KeyAttributeCollection keyAttributes)
		{
			var fields = keyAttributes
				.Select(a => new { Name = a.Key, Value = FormatValue(a.Value) })
				.ToList();

			if (fields.Count == 0)
			{
				output.WriteLine("(no key attributes)");
				return;
			}

			output.WriteTable(
				fields,
				() => ["Field", "Value"],
				row => [row.Name, row.Value]);
		}

		private void PrintEntityFields(Entity entity)
		{
			var fields = entity.Attributes
				.Select(a => new { Name = a.Key, Value = FormatValue(a.Value) })
				.ToList();

			if (entity.Id != Guid.Empty)
			{
				fields.Insert(0, new { Name = "id", Value = entity.Id.ToString() });
			}

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
