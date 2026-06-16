using System.ServiceModel;
using System.Text.Json;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class RunCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<RunCustomApiCommand>
	{
		public async Task<CommandResult> ExecuteAsync(RunCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// ── 1. Resolve Custom API ─────────────────────────────────────────────
				output.Write("Resolving Custom API '");
				output.Write(command.UniqueName!, ConsoleColor.Yellow);
				output.Write("'...");

				var apiQ = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
				apiQ.ColumnSet.AddColumns("customapiid", "uniquename", "isfunction", "plugintypeid");
				apiQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.UniqueName);
				var apiResult = await crm.RetrieveMultipleAsync(apiQ);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.UniqueName}' not found.");
				}
				var apiId      = apiResult.Entities[0].Id;
				var pluginRef  = apiResult.Entities[0].GetAttributeValue<EntityReference>("plugintypeid");
				output.WriteLine("Done", ConsoleColor.Green);

				if (pluginRef == null)
					output.WriteLine("  Warning: this Custom API has no plugin bound — execution may fail.", ConsoleColor.Yellow);

				// ── 2. Load parameter metadata ────────────────────────────────────────
				var paramQ = new QueryExpression("customapirequestparameter") { NoLock = true };
				paramQ.ColumnSet.AddColumns("uniquename", "type", "isoptional");
				paramQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
				var paramMeta = (await crm.RetrieveMultipleAsync(paramQ)).Entities;

				// ── 3. Parse input JSON ───────────────────────────────────────────────
				var inputJson = await ResolveInputJsonAsync(command);
				var userInput = ParseJson(inputJson);  // null when no input provided

				// ── 4. Validate required params and build the request ─────────────────
				var apiUniqueName = command.UniqueName!;
				var inPrefix = apiUniqueName + "-in-";

				var request = new OrganizationRequest(apiUniqueName);

				foreach (var p in paramMeta)
				{
					var fullName  = p.GetAttributeValue<string>("uniquename") ?? "";
					var shortName = fullName.StartsWith(inPrefix, StringComparison.OrdinalIgnoreCase)
						? fullName[inPrefix.Length..]
						: fullName;
					var isOptional = p.GetAttributeValue<bool>("isoptional");
					var typeCode   = p.GetAttributeValue<OptionSetValue>("type")?.Value ?? -1;

					// Find matching key in user input (case-insensitive on short name)
					JsonElement element = default;
					var found = userInput.HasValue && TryFindKey(userInput.Value, shortName, out element);

					if (!found && !isOptional)
						return CommandResult.Fail($"Required parameter '{shortName}' is missing from the input.");

					if (found)
						request[fullName] = ConvertValue(element, typeCode, shortName);
				}

				// Warn about keys in user input that don't match any parameter
				if (userInput.HasValue)
				{
					var knownShortNames = paramMeta
						.Select(p => p.GetAttributeValue<string>("uniquename") ?? "")
						.Select(n => n.StartsWith(inPrefix, StringComparison.OrdinalIgnoreCase) ? n[inPrefix.Length..] : n)
						.ToHashSet(StringComparer.OrdinalIgnoreCase);

					foreach (var key in userInput.Value.EnumerateObject().Select(p => p.Name))
					{
						if (!knownShortNames.Contains(key))
							output.WriteLine($"  Warning: input key '{key}' does not match any declared parameter — ignored.", ConsoleColor.Yellow);
					}
				}

				// ── 5. Execute ────────────────────────────────────────────────────────
				output.Write("Executing '");
				output.Write(apiUniqueName, ConsoleColor.Yellow);
				output.Write("'...");

				var response = await crm.ExecuteAsync(request);
				output.WriteLine("Done", ConsoleColor.Green);

				// ── 6. Display response ───────────────────────────────────────────────
				var outPrefix = apiUniqueName + "-out-";
				var responseEntries = response.Results
					.Select(kv => (
						ShortName: kv.Key.StartsWith(outPrefix, StringComparison.OrdinalIgnoreCase)
							? kv.Key[outPrefix.Length..]
							: kv.Key,
						FullName: kv.Key,
						Value: kv.Value))
					.ToList();

				if (responseEntries.Count == 0)
				{
					output.WriteLine();
					output.WriteLine("  (no response values returned)", ConsoleColor.DarkGray);
				}
				else
				{
					output.WriteLine();
					output.WriteTable(
						responseEntries,
						() => ["Name", "Value"],
						row => [row.ShortName, FormatValue(row.Value)],
						(col, _) => col == 0 ? ConsoleColor.White : (ConsoleColor?)null);
				}

				var result = CommandResult.Success();
				foreach (var (shortName, fullName, value) in responseEntries)
					result[shortName] = value;

				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		private static async Task<string?> ResolveInputJsonAsync(RunCustomApiCommand command)
		{
			if (!string.IsNullOrWhiteSpace(command.Input))
				return command.Input;

			if (!string.IsNullOrWhiteSpace(command.InputFile))
			{
				var path = command.InputFile.Trim();
				if (!File.Exists(path))
					throw new FileNotFoundException($"Input file not found: {path}", path);
				return await File.ReadAllTextAsync(path);
			}

			return null;
		}

		private static JsonElement? ParseJson(string? json)
		{
			if (string.IsNullOrWhiteSpace(json)) return null;

			var doc = JsonDocument.Parse(json);
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
				throw new InvalidOperationException("Input JSON must be a JSON object ({...}).");

			return doc.RootElement;
		}

		private static bool TryFindKey(JsonElement obj, string shortName, out JsonElement value)
		{
			foreach (var prop in obj.EnumerateObject())
			{
				if (string.Equals(prop.Name, shortName, StringComparison.OrdinalIgnoreCase))
				{
					value = prop.Value;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Maps a JSON element to the appropriate Dataverse SDK type using the CustomApiParamType code.
		/// </summary>
		private static object? ConvertValue(JsonElement el, int typeCode, string paramName)
		{
			try
			{
				return typeCode switch
				{
					0  => el.GetBoolean(),                                    // Boolean
					1  => el.GetDateTime(),                                   // DateTime
					2  => el.GetDecimal(),                                    // Decimal
					3  => ToEntity(el),                                       // Entity
					4  => ToEntityCollection(el),                             // EntityCollection
					5  => ToEntityReference(el),                              // EntityReference
					6  => el.GetDouble(),                                     // Float
					7  => el.GetInt32(),                                      // Integer
					8  => new Money(el.GetDecimal()),                         // Money
					9  => new OptionSetValue(el.GetInt32()),                  // Picklist
					10 => el.GetString(),                                     // String
					11 => el.EnumerateArray().Select(e => e.GetString() ?? "").ToArray(), // StringArray
					12 => el.GetGuid(),                                       // Guid
					_  => el.GetString()
				};
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Cannot convert value for parameter '{paramName}': {ex.Message}", ex);
			}
		}

		/// <summary>Expects {"logicalname":"...", "id":"..."}</summary>
		private static EntityReference ToEntityReference(JsonElement el)
		{
			var logicalName = el.GetProperty("logicalname").GetString()
				?? throw new InvalidOperationException("EntityReference requires 'logicalname'.");
			var id = el.GetProperty("id").GetGuid();
			return new EntityReference(logicalName, id);
		}

		/// <summary>Expects {"logicalname":"...", "id":"..."} with optional attributes array.</summary>
		private static Entity ToEntity(JsonElement el)
		{
			var logicalName = el.GetProperty("logicalname").GetString()
				?? throw new InvalidOperationException("Entity requires 'logicalname'.");
			var id = el.TryGetProperty("id", out var idEl) ? idEl.GetGuid() : Guid.Empty;
			return new Entity(logicalName, id);
		}

		/// <summary>Expects an array of entity-reference objects.</summary>
		private static EntityCollection ToEntityCollection(JsonElement el)
		{
			var entities = el.EnumerateArray().Select(ToEntity).ToList();
			return new EntityCollection(entities);
		}

		private static string FormatValue(object? value) => value switch
		{
			null                     => "(null)",
			Money m                  => m.Value.ToString("F2"),
			OptionSetValue osv       => osv.Value.ToString(),
			EntityReference er       => $"{er.LogicalName}({er.Id})",
			Entity e                 => $"{e.LogicalName}({e.Id})",
			EntityCollection ec      => $"[{ec.Entities.Count} entities]",
			string[] arr             => string.Join(", ", arr),
			_                        => value.ToString() ?? ""
		};

		private static string ShortName(string uniqueName, string prefix)
			=> uniqueName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				? uniqueName[prefix.Length..]
				: uniqueName;
	}
}
