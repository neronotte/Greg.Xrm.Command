using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Data
{
	public class DataExportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<DataExportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DataExportCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				var tables = command.Tables;
				if (command.SolutionUniqueName != null)
				{
					output.WriteLine($"Resolving tables from solution: {command.SolutionUniqueName}");
					tables = await GetSolutionTablesAsync(crm, command.SolutionUniqueName, cancellationToken);
					if (tables.Length == 0)
					{
						return CommandResult.Fail($"No tables found in solution '{command.SolutionUniqueName}'.");
					}
					output.WriteLine($"  Found {tables.Length} table(s) in solution");
				}

				if (!Directory.Exists(command.OutputPath))
				{
					Directory.CreateDirectory(command.OutputPath);
				}

				output.WriteLine($"Exporting {tables.Length} table(s) to {command.OutputPath}", ConsoleColor.Cyan);
				output.WriteLine($"  Format: {command.Format}");
				output.WriteLine($"  Batch size: {command.BatchSize}");
				output.WriteLine();

				var totalExported = 0;

				foreach (var table in tables)
				{
					output.Write($"  Exporting {table}...");
					var count = await ExportTableAsync(crm, table, command.OutputPath, command.Format, command.BatchSize, cancellationToken);
					output.WriteLine($" {count} records", ConsoleColor.Green);
					totalExported += count;
				}

				output.WriteLine();
				output.WriteLine($"Export complete: {totalExported} total records", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error exporting data: {ex.Message}", ex);
			}
		}

		private async Task<int> ExportTableAsync(IOrganizationServiceAsync2 crm, string table, string outputPath, string format, int batchSize, CancellationToken ct)
		{
			var records = new List<Dictionary<string, object?>>();
			var page = 0;

			while (true)
			{
				var query = new QueryExpression(table);
				query.ColumnSet.AllColumns = true;
				query.PageInfo.Count = batchSize;
				query.PageInfo.PageNumber = page + 1;
				query.AddOrder("createdon", OrderType.Descending);

				var results = await crm.RetrieveMultipleAsync(query, ct);
				foreach (var entity in results.Entities)
				{
					var record = new Dictionary<string, object?>();
					foreach (var attr in entity.Attributes)
					{
						record[attr.Key] = SerializeValue(attr.Value);
					}
					records.Add(record);
				}

				if (!results.MoreRecords) break;
				page++;
			}

			var fileName = Path.Combine(outputPath, $"{table}.{format}");
			var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = format == "json" });
			await File.WriteAllTextAsync(fileName, json, ct);

			return records.Count;
		}

		private async Task<string[]> GetSolutionTablesAsync(IOrganizationServiceAsync2 crm, string solutionName, CancellationToken ct)
		{
			var query = new QueryExpression("solutioncomponent");
			query.ColumnSet.AddColumn("objectid");
			query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionName);
			query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, 1); // Entity type

			var results = await crm.RetrieveMultipleAsync(query, ct);
			return results.Entities
				.Select(e => e.GetAttributeValue<EntityReference>("objectid")?.Name)
				.Where(n => !string.IsNullOrEmpty(n))
				.ToArray()!;
		}

		private static object? SerializeValue(object value)
		{
			return value switch
			{
				EntityReference er => new { Id = er.Id, LogicalName = er.LogicalName, Name = er.Name },
				OptionSetValue ov => ov.Value,
				Money m => m.Value,
				DateTime dt => dt.ToString("o"),
				_ => value,
			};
		}
	}

	public class DataImportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<DataImportCommand>
	{
		private readonly IOutput output = output ?? throw new ArgumentNullException(nameof(output));
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));

		public async Task<CommandResult> ExecuteAsync(DataImportCommand command, CancellationToken cancellationToken)
		{
			try
			{
				if (!File.Exists(command.InputPath) && !Directory.Exists(command.InputPath))
				{
					return CommandResult.Fail($"Input path not found: {command.InputPath}");
				}

				this.output.Write("Connecting to Dataverse...");
				var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync(cancellationToken);
				this.output.WriteLine(" Done", ConsoleColor.Green);

				if (command.DryRun)
				{
					this.output.WriteLine("[DRY RUN] Would import data from:", ConsoleColor.Yellow);
					this.output.WriteLine($"  Input: {command.InputPath}");
					this.output.WriteLine($"  Format: {command.Format}");
					this.output.WriteLine($"  Mode: {command.Mode}");
					return CommandResult.Success();
				}

				this.output.WriteLine($"Importing data from {command.InputPath}", ConsoleColor.Cyan);
				
				var files = new List<string>();
				if (Directory.Exists(command.InputPath))
				{
					files.AddRange(Directory.GetFiles(command.InputPath, $"*.{command.Format}"));
				}
				else
				{
					files.Add(command.InputPath);
				}

				foreach (var file in files)
				{
					var tableName = command.TargetTable ?? Path.GetFileNameWithoutExtension(file);
					this.output.Write($"  Importing {tableName}...");
					
					var content = await File.ReadAllTextAsync(file, cancellationToken);
					var records = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(content);
					
					if (records == null) continue;

					var count = 0;
					foreach (var record in records)
					{
						var entity = new Entity(tableName);
						foreach (var kvp in record)
						{
							entity[kvp.Key] = DeserializeValue(kvp.Value);
						}
						
						if (command.Mode == "upsert" && entity.Attributes.ContainsKey(tableName + "id"))
						{
							// Upsert logic
							entity.Id = (Guid)entity[tableName + "id"];
							await crm.UpdateAsync(entity, cancellationToken);
						}
						else
						{
							await crm.CreateAsync(entity, cancellationToken);
						}
						count++;
					}
					this.output.WriteLine($" {count} records", ConsoleColor.Green);
				}

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail($"Error importing data: {ex.Message}", ex);
			}
		}

		private static object? DeserializeValue(JsonElement element)
		{
			return element.ValueKind switch
			{
				JsonValueKind.String => element.GetString(),
				JsonValueKind.Number => element.GetDecimal(),
				JsonValueKind.True => true,
				JsonValueKind.False => false,
				JsonValueKind.Null => null,
				JsonValueKind.Object => DeserializeObject(element),
				_ => element.GetRawText()
			};
		}

		private static object? DeserializeObject(JsonElement element)
		{
			if (element.TryGetProperty("Id", out var idProp) && element.TryGetProperty("LogicalName", out var nameProp))
			{
				return new EntityReference(nameProp.GetString(), idProp.GetGuid());
			}
			return element.GetRawText();
		}
	}
}
