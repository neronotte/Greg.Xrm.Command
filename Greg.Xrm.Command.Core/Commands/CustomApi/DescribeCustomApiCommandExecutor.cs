using System.ServiceModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class DescribeCustomApiCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository)
		: ICommandExecutor<DescribeCustomApiCommand>
	{
		private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

		public async Task<CommandResult> ExecuteAsync(DescribeCustomApiCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Resolve the Custom API
				output.Write($"Resolving Custom API '");
				output.Write(command.UniqueName!, ConsoleColor.Yellow);
				output.Write("'...");

				var apiQ = new QueryExpression("customapi") { NoLock = true, TopCount = 1 };
				apiQ.ColumnSet.AddColumns("customapiid", "uniquename", "displayname", "description",
					"isfunction", "isprivate", "bindingtype", "allowedcustomprocessingsteptype",
					"executeprivilegename", "plugintypeid");
				apiQ.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.UniqueName);

				var apiResult = await crm.RetrieveMultipleAsync(apiQ);
				if (apiResult.Entities.Count == 0)
				{
					output.WriteLine("Not found", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.UniqueName}' not found.");
				}

				var api = apiResult.Entities[0];
				var apiId = api.Id;
				output.WriteLine("Done", ConsoleColor.Green);

				// Retrieve request parameters
				var paramQ = new QueryExpression("customapirequestparameter") { NoLock = true };
				paramQ.ColumnSet.AddColumns("name", "uniquename", "displayname", "description", "type", "isoptional");
				paramQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
				paramQ.AddOrder("uniquename", OrderType.Ascending);
				var paramResult = await crm.RetrieveMultipleAsync(paramQ);

				// Retrieve response properties
				var respQ = new QueryExpression("customapiresponseproperty") { NoLock = true };
				respQ.ColumnSet.AddColumns("name", "uniquename", "displayname", "description", "type");
				respQ.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
				respQ.AddOrder("uniquename", OrderType.Ascending);
				var respResult = await crm.RetrieveMultipleAsync(respQ);

				// ── Print header ──────────────────────────────────────────────────────
				output.WriteLine();

				var uniqueName  = api.GetAttributeValue<string>("uniquename") ?? "";
				var displayName = api.GetAttributeValue<string>("displayname") ?? "";
				var description = api.GetAttributeValue<string>("description");
				var isFunction  = api.GetAttributeValue<bool>("isfunction");
				var isPrivate   = api.GetAttributeValue<bool>("isprivate");
				var bindingType = BindingTypeLabel(api.GetAttributeValue<OptionSetValue>("bindingtype"));
				var stepType    = StepTypeLabel(api.GetAttributeValue<OptionSetValue>("allowedcustomprocessingsteptype"));
				var privilege   = api.GetAttributeValue<string>("executeprivilegename");
				var pluginRef   = api.GetAttributeValue<EntityReference>("plugintypeid");

				output.WriteLine($"Custom API:   ", ConsoleColor.DarkGray);
				output.Write("  Unique Name:  "); output.WriteLine(uniqueName, ConsoleColor.White);
				output.Write("  Display Name: "); output.WriteLine(displayName, ConsoleColor.White);
				output.Write("  Type:         "); output.WriteLine(isFunction ? "Function (GET)" : "Action (POST)", isFunction ? ConsoleColor.Cyan : ConsoleColor.Green);
				output.Write("  Binding:      "); output.WriteLine(bindingType, ConsoleColor.White);
				output.Write("  Private:      "); output.WriteLine(isPrivate ? "Yes" : "No", isPrivate ? ConsoleColor.Yellow : ConsoleColor.White);
				output.Write("  Step Types:   "); output.WriteLine(stepType, ConsoleColor.White);
				output.Write("  Privilege:    "); output.WriteLine(string.IsNullOrWhiteSpace(privilege) ? "(none)" : privilege, ConsoleColor.White);
				output.Write("  Plugin:       "); output.WriteLine(pluginRef?.Name ?? "(unbound)", pluginRef != null ? ConsoleColor.Green : ConsoleColor.Yellow);
				if (!string.IsNullOrWhiteSpace(description))
				{
					output.Write("  Description:  "); output.WriteLine(description, ConsoleColor.White);
				}

				// ── Signature line ────────────────────────────────────────────────────
					output.WriteLine();

					var inputParams = paramResult.Entities
							.OrderBy(p => p.GetAttributeValue<bool>("isoptional"))
							.Select(p => (
								name: ParamName(p),
								type: TypeLabel(p.GetAttributeValue<OptionSetValue>("type")),
								opt:  p.GetAttributeValue<bool>("isoptional")));

					var outputParams = respResult.Entities
							.Select(r => (
								name: ParamName(r),
								type: TypeLabel(r.GetAttributeValue<OptionSetValue>("type"))));

					output.Write("  Signature:    ", ConsoleColor.DarkGray);
					CustomApiSignatureWriter.WriteSignature(output, uniqueName, inputParams, outputParams);
					output.WriteLine();

				// ── Request parameters table ──────────────────────────────────────────
				if (paramResult.Entities.Count > 0)
				{
					output.WriteLine();
					output.WriteLine("Request Parameters:", ConsoleColor.DarkGray);
					output.WriteTable(
						paramResult.Entities,
						() => ["Name", "Type", "Required", "Description"],
						row => [
							ParamName(row),
							TypeLabel(row.GetAttributeValue<OptionSetValue>("type")),
							row.GetAttributeValue<bool>("isoptional") ? "No" : "Yes",
							row.GetAttributeValue<string>("description") ?? ""
						],
						(col, _) => col switch
						{
							0 => ConsoleColor.White,
							1 => ConsoleColor.Cyan,
							2 => (ConsoleColor?)null,
							_ => ConsoleColor.DarkGray
						});
				}

				// ── Response properties table ─────────────────────────────────────────
				if (respResult.Entities.Count > 0)
				{
					output.WriteLine();
					output.WriteLine("Response Properties:", ConsoleColor.DarkGray);
					output.WriteTable(
						respResult.Entities,
						() => ["Name", "Type", "Description"],
						row => [
							ParamName(row),
							TypeLabel(row.GetAttributeValue<OptionSetValue>("type")),
							row.GetAttributeValue<string>("description") ?? ""
						],
						(col, _) => col switch
						{
							0 => ConsoleColor.White,
							1 => ConsoleColor.Cyan,
							_ => ConsoleColor.DarkGray
						});
				}

				if (paramResult.Entities.Count == 0 && respResult.Entities.Count == 0)
				{
					output.WriteLine();
					output.WriteLine("  No request parameters or response properties defined.", ConsoleColor.DarkGray);
				}

				// ── Generate input file ───────────────────────────────────────────────
					if (command.GenerateInputFile != null)
				{
						var inputPath = string.IsNullOrWhiteSpace(command.GenerateInputFile)
							? $"{uniqueName}-input.json"
							: command.GenerateInputFile;
						var inputJson = BuildSampleInput(paramResult.Entities);
						await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(inputJson, IndentedJson), cancellationToken);
						output.WriteLine();
						output.Write("  Sample input written to: ");
						output.WriteLine(inputPath, ConsoleColor.Green);
					}

					// ── Generate schema file ──────────────────────────────────────────────
					if (command.GenerateSchemaFile != null)
					{
						var schemaPath = string.IsNullOrWhiteSpace(command.GenerateSchemaFile)
							? $"{uniqueName}-schema.json"
							: command.GenerateSchemaFile;
						var schema = BuildJsonSchema(uniqueName, description, paramResult.Entities);
						await File.WriteAllTextAsync(schemaPath, JsonSerializer.Serialize(schema, IndentedJson), cancellationToken);
						output.WriteLine();
						output.Write("  JSON Schema written to: ");
						output.WriteLine(schemaPath, ConsoleColor.Green);
					}

				var result = CommandResult.Success();
				result["UniqueName"]     = uniqueName;
				result["DisplayName"]    = displayName;
				result["Type"]           = isFunction ? "Function" : "Action";
				result["ParameterCount"] = paramResult.Entities.Count;
				result["ResponseCount"]  = respResult.Entities.Count;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		// ── JSON generation ───────────────────────────────────────────────────────

		/// <summary>
		/// Produces a sample JSON object with one representative value per parameter.
		/// All parameters (required and optional) are included so the user can see every option.
		/// </summary>
		private static JsonObject BuildSampleInput(IEnumerable<Entity> parameters)
		{
			var obj = new JsonObject();
			foreach (var p in parameters)
			{
				var pName    = ParamName(p);
				var typeCode = p.GetAttributeValue<OptionSetValue>("type")?.Value ?? -1;
				obj[pName] = SampleValue(typeCode);
			}
			return obj;
		}

		/// <summary>
		/// Produces a JSON Schema (draft 2020-12) for the input parameters of the Custom API.
		/// </summary>
		private static JsonObject BuildJsonSchema(string apiUniqueName, string? apiDescription, IEnumerable<Entity> parameters)
		{
			var schema = new JsonObject
			{
				["$schema"]     = "https://json-schema.org/draft/2020-12/schema",
				["title"]       = $"{apiUniqueName} — input parameters",
				["description"] = apiDescription ?? $"Input parameters for the {apiUniqueName} Custom API.",
				["type"]        = "object"
			};

			var properties = new JsonObject();
			var required   = new JsonArray();

			foreach (var p in parameters)
			{
					var pName      = ParamName(p);
				var typeCode   = p.GetAttributeValue<OptionSetValue>("type")?.Value ?? -1;
				var isOptional = p.GetAttributeValue<bool>("isoptional");
				var desc       = p.GetAttributeValue<string>("description");

				var propSchema = TypeSchema(typeCode);
				if (!string.IsNullOrWhiteSpace(desc))
					propSchema["description"] = desc;

					properties[pName] = propSchema;

				if (!isOptional)
						required.Add(pName);
			}

			schema["properties"] = properties;
			if (required.Count > 0)
				schema["required"] = required;

			return schema;
		}

		private static JsonNode SampleValue(int typeCode) => typeCode switch
		{
			0  => JsonValue.Create(false)!,                          // Boolean
			1  => JsonValue.Create("2024-01-01T00:00:00Z")!,         // DateTime
			2  => JsonValue.Create(0.0)!,                            // Decimal
			3  => EntityObject(),                                    // Entity
			4  => new JsonArray { EntityObject() },                  // EntityCollection
			5  => EntityObject(),                                    // EntityReference
			6  => JsonValue.Create(0.0)!,                            // Float
			7  => JsonValue.Create(0)!,                              // Integer
			8  => JsonValue.Create(0.00)!,                           // Money
			9  => JsonValue.Create(0)!,                              // Picklist
			10 => JsonValue.Create("")!,                             // String
			11 => new JsonArray { JsonValue.Create("")! },           // StringArray
			12 => JsonValue.Create("00000000-0000-0000-0000-000000000000")!, // Guid
			_  => JsonValue.Create("")!
		};

		private static JsonObject TypeSchema(int typeCode) => typeCode switch
		{
			0  => new JsonObject { ["type"] = "boolean" },
			1  => new JsonObject { ["type"] = "string",  ["format"] = "date-time" },
			2  => new JsonObject { ["type"] = "number" },
			3  => EntitySchema(),
			4  => new JsonObject { ["type"] = "array",   ["items"] = EntitySchema() },
			5  => EntitySchema(),
			6  => new JsonObject { ["type"] = "number" },
			7  => new JsonObject { ["type"] = "integer" },
			8  => new JsonObject { ["type"] = "number",  ["description"] = "Money value" },
			9  => new JsonObject { ["type"] = "integer", ["description"] = "OptionSet integer value" },
			10 => new JsonObject { ["type"] = "string" },
			11 => new JsonObject { ["type"] = "array",   ["items"] = new JsonObject { ["type"] = "string" } },
			12 => new JsonObject { ["type"] = "string",  ["format"] = "uuid" },
			_  => new JsonObject { ["type"] = "string" }
		};

		private static JsonObject EntityObject() => new()
		{
			["logicalname"] = "account",
			["id"]          = "00000000-0000-0000-0000-000000000000"
		};

		private static JsonObject EntitySchema() => new()
		{
			["type"] = "object",
			["properties"] = new JsonObject
			{
				["logicalname"] = new JsonObject { ["type"] = "string" },
				["id"]          = new JsonObject { ["type"] = "string", ["format"] = "uuid" }
			},
			["required"] = new JsonArray { "logicalname", "id" }
		};

		// ── Label helpers ─────────────────────────────────────────────────────────

			/// <summary>
			/// Returns the user-facing parameter name: the uniquename attribute,
			/// which is the clean identifier the user specified (e.g. "Addend1").
			/// Falls back to the name attribute if uniquename is absent.
			/// </summary>
			private static string ParamName(Entity e)
				=> e.GetAttributeValue<string>("uniquename")
				   ?? e.GetAttributeValue<string>("name")
				   ?? "";

			private static string ShortName(string uniqueName, string prefix)
			=> uniqueName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				? uniqueName[prefix.Length..]
				: uniqueName;

		private static string TypeLabel(OptionSetValue? value)
		{
			if (value == null) return "Unknown";
			foreach (var t in Enum.GetValues<CustomApiParamType>())
			{
				var spec = new CustomApiParamSpec("_", t, false);
				if (spec.TypeCode == value.Value)
					return t.ToString();
			}
			return $"Unknown({value.Value})";
		}

		private static string BindingTypeLabel(OptionSetValue? value) => value?.Value switch
		{
			0 => "Global",
			1 => "Entity",
			2 => "EntityCollection",
			_ => "Unknown"
		};

		private static string StepTypeLabel(OptionSetValue? value) => value?.Value switch
		{
			0 => "None",
			1 => "Async Only",
			2 => "Sync and Async",
			_ => "Unknown"
		};
	}
}
