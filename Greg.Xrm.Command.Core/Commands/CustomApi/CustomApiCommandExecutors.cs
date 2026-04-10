using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.CustomApi
{
	public class CustomApiCreateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<CustomApiCreateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CustomApiCreateCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Create Custom API
				var customApi = new Entity("customapi");
				customApi["uniquename"] = command.Name;
				customApi["displayname"] = command.DisplayName ?? command.Name;
				customApi["description"] = command.Description ?? "";
				customApi["iscustomapivisible"] = true;
				customApi["iscustomprocessed"] = false;

				// Binding type
				customApi["boundentitylogicalname"] = command.EntityLogicalName;
				customApi["customapirequestprocessingtype"] = command.BindingType switch
				{
					"Entity" => 0,
					"EntityCollection" => 1,
					_ => 2 // Global
				};

				customApi["isfunction"] = command.IsFunction;

				if (!string.IsNullOrEmpty(command.ExecutePluginTypeName))
				{
					var pluginTypeQuery = new QueryExpression("plugintype");
					pluginTypeQuery.ColumnSet.AddColumn("plugintypeid");
					pluginTypeQuery.Criteria.AddCondition("typename", ConditionOperator.Equal, command.ExecutePluginTypeName);
					var pluginResult = await crm.RetrieveMultipleAsync(pluginTypeQuery, cancellationToken);
					if (pluginResult.Entities.Count > 0)
					{
						customApi["workflowsdksteppluginTypeId"] = new EntityReference("plugintype", pluginResult.Entities[0].Id);
					}
				}

				output.Write($"Creating Custom API '{command.Name}'...");
				var apiId = await crm.CreateAsync(customApi, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);

				// Create input parameters
				if (command.Inputs != null && command.Inputs.Length > 0)
				{
					foreach (var inputDef in command.Inputs)
					{
						await CreateParameterAsync(crm, apiId, inputDef, true, cancellationToken);
					}
				}

				// Create output parameters
				if (command.Outputs != null && command.Outputs.Length > 0)
				{
					foreach (var outputDef in command.Outputs)
					{
						await CreateParameterAsync(crm, apiId, outputDef, false, cancellationToken);
					}
				}

				output.WriteLine($"Custom API '{command.Name}' created successfully (ID: {apiId}).", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to create Custom API: {ex.Message}", ex);
			}
		}

		private async Task CreateParameterAsync(IOrganizationServiceAsync2 crm, Guid customApiId, string paramDef, bool isInput, CancellationToken ct)
		{
			var parts = paramDef.Split(':');
			if (parts.Length != 2)
			{
				output.WriteLine($"  WARNING: Invalid parameter format '{paramDef}'. Expected 'Type:Name'.", ConsoleColor.Yellow);
				return;
			}

			var typeName = parts[0];
			var paramName = parts[1];

			var param = new Entity(isInput ? "customapirequestparameter" : "customapiresponseproperty");
			param["customapiid"] = new EntityReference("customapi", customApiId);
			param["uniquename"] = paramName;
			param["displayname"] = paramName;

			// Map type name to Dataverse type code
			var typeCode = typeName.ToLowerInvariant() switch
			{
				"string" => 10,
				"int" or "integer" => 6,
				"bool" or "boolean" => 7,
				"datetime" => 8,
				"decimal" or "money" => 5,
				"double" => 4,
				"guid" or "uniqueidentifier" => 11,
				"entity" or "entityreference" => 1,
				"picklist" or "optionset" => 2,
				"stringarray" => 13,
				_ => 10 // Default to String
			};

			param["type"] = typeCode;

			await crm.CreateAsync(param, ct);
			output.WriteLine($"  Created {(isInput ? "input" : "output")} parameter: {paramName} ({typeName})", ConsoleColor.Green);
		}
	}

	public class CustomApiListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<CustomApiListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CustomApiListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var query = new QueryExpression("customapi");
				query.ColumnSet.AddColumns("uniquename", "displayname", "description", "boundentitylogicalname", "iscustomprocessed", "isfunction", "createdon");
				query.AddOrder("uniquename", OrderType.Ascending);

				if (!string.IsNullOrEmpty(command.EntityLogicalName))
				{
					query.Criteria.AddCondition("boundentitylogicalname", ConditionOperator.Equal, command.EntityLogicalName);
				}

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (result.Entities.Count == 0)
				{
					output.WriteLine("No Custom APIs found.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				if (command.Format == "json")
				{
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(
						result.Entities.Select(MapToJson).ToList(),
						Newtonsoft.Json.Formatting.Indented);
					output.WriteLine(json);
				}
				else
				{
					output.WriteTable(result.Entities,
						() => new[] { "Name", "Display Name", "Bound Entity", "Function", "Created" },
						e => new[] {
							e.GetAttributeValue<string>("uniquename") ?? "-",
							e.GetAttributeValue<string>("displayname") ?? "-",
							e.GetAttributeValue<string>("boundentitylogicalname") ?? "Global",
							e.GetAttributeValue<bool?>("isfunction") == true ? "Yes" : "No",
							e.GetAttributeValue<DateTime?>("createdon")?.ToString("yyyy-MM-dd") ?? "-"
						}
					);
				}

				output.WriteLine($"\nTotal: {result.Entities.Count} Custom API(s)", ConsoleColor.Cyan);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to list Custom APIs: {ex.Message}", ex);
			}
		}

		private static object MapToJson(Entity e) => new
		{
			UniqueName = e.GetAttributeValue<string>("uniquename"),
			DisplayName = e.GetAttributeValue<string>("displayname"),
			Description = e.GetAttributeValue<string>("description"),
			BoundEntity = e.GetAttributeValue<string>("boundentitylogicalname"),
			IsFunction = e.GetAttributeValue<bool?>("isfunction") ?? false,
			CreatedOn = e.GetAttributeValue<DateTime?>("createdon")
		};
	}

	public class CustomApiDeleteCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<CustomApiDeleteCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CustomApiDeleteCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var query = new QueryExpression("customapi");
				query.ColumnSet.AddColumn("customapiid");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.Name);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (result.Entities.Count == 0)
				{
					output.WriteLine($"Custom API '{command.Name}' not found.", ConsoleColor.Red);
					return CommandResult.Fail($"Custom API '{command.Name}' not found.");
				}

				var apiRef = result.Entities[0].ToEntityReference();
				output.Write($"Deleting Custom API '{command.Name}'...");
				await crm.DeleteAsync(apiRef.LogicalName, apiRef.Id, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to delete Custom API: {ex.Message}", ex);
			}
		}
	}
}
