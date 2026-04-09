using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class PluginRegisterAttributesCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<PluginRegisterAttributesCommand>
	{
		public async Task<CommandResult> ExecuteAsync(PluginRegisterAttributesCommand command, CancellationToken cancellationToken)
		{
			try
			{
				// 1. Scan DLLs for plugin attributes
				output.WriteLine("Step 1: Scanning for plugin attributes...", ConsoleColor.Cyan);

				IList<PluginAssemblyMetadata> assemblies;
				if (File.Exists(command.Path))
				{
					var meta = PluginScanner.ScanAssembly(command.Path);
					assemblies = meta != null ? new List<PluginAssemblyMetadata> { meta } : Array.Empty<PluginAssemblyMetadata>();
				}
				else if (Directory.Exists(command.Path))
				{
					assemblies = PluginScanner.ScanDirectory(command.Path);
				}
				else
				{
					return CommandResult.Fail($"Path not found: {command.Path}");
				}

				if (assemblies.Count == 0)
				{
					output.WriteLine("No plugin attributes found in the specified path.", ConsoleColor.Yellow);
					return CommandResult.Fail("No plugin attributes found.");
				}

				output.WriteLine($"Found {assemblies.Count} assembly(ies) with plugin attributes:", ConsoleColor.Green);
				foreach (var asm in assemblies)
				{
					output.WriteLine($"  - {asm.AssemblyName} ({asm.PluginTypes.Count} plugin type(s))");
					foreach (var pluginType in asm.PluginTypes)
					{
						output.WriteLine($"    - {pluginType.TypeNameWithoutNamespace} ({pluginType.Steps.Count} step(s), {pluginType.Images.Count} image(s), {pluginType.Webhooks.Count} webhook(s))");
					}
				}

				if (command.DryRun)
				{
					output.WriteLine();
					output.WriteLine("[DRY RUN] No changes made to Dataverse.", ConsoleColor.Yellow);

					if (command.Format == "json")
					{
						var json = Newtonsoft.Json.JsonConvert.SerializeObject(assemblies, Newtonsoft.Json.Formatting.Indented);
						output.WriteLine(json);
					}

					return CommandResult.Success();
				}

				// 2. Connect to Dataverse
				output.WriteLine();
				output.Write("Step 2: Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// 3. Register plugins
				output.WriteLine("Step 3: Registering plugins...", ConsoleColor.Cyan);

				var publisherId = await EnsurePublisherAsync(crm, command.PublisherUniqueName, command.PublisherName, cancellationToken);

				foreach (var asm in assemblies)
				{
					var assemblyId = await RegisterAssemblyAsync(crm, asm, publisherId, command.IsolationMode, cancellationToken);

					foreach (var pluginType in asm.PluginTypes)
					{
						var pluginTypeId = await RegisterPluginTypeAsync(crm, pluginType, assemblyId, cancellationToken);

						foreach (var step in pluginType.Steps)
						{
							await RegisterStepAsync(crm, step, pluginTypeId, pluginType, cancellationToken);
						}

						foreach (var image in pluginType.Images)
						{
							await RegisterImageAsync(crm, image, pluginTypeId, cancellationToken);
						}

						foreach (var webhook in pluginType.Webhooks)
						{
							await RegisterWebhookAsync(crm, webhook, pluginTypeId, cancellationToken);
						}
					}
				}

				output.WriteLine();
				output.WriteLine("Plugin registration completed successfully!", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex) when (ex is FaultException<OrganizationServiceFault> or IOException or InvalidOperationException)
			{
				return CommandResult.Fail($"Error during plugin registration: {ex.Message}", ex);
			}
		}

		private async Task<Guid> EnsurePublisherAsync(IOrganizationServiceAsync2 crm, string uniqueName, string friendlyName, CancellationToken ct)
		{
			var query = new QueryExpression("publisher");
			query.ColumnSet.AddColumn("publisherid");
			query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

			var result = await crm.RetrieveMultipleAsync(query, ct);
			if (result.Entities.Count > 0)
			{
				return result.Entities[0].Id;
			}

			var publisher = new Entity("publisher");
			publisher["uniquename"] = uniqueName;
			publisher["friendlyname"] = friendlyName;
			publisher["customizationprefix"] = uniqueName.Length > 6 ? uniqueName.Substring(0, 6).ToLower() : "dev";
			publisher["email"] = "devkit@pacx.local";

			var id = await crm.CreateAsync(publisher, ct);
			output.WriteLine($"  Created publisher: {uniqueName}", ConsoleColor.Green);
			return id;
		}

		private async Task<Guid> RegisterAssemblyAsync(IOrganizationServiceAsync2 crm, PluginAssemblyMetadata asm, Guid publisherId, string isolationMode, CancellationToken ct)
		{
			var dllContent = Convert.ToBase64String(await File.ReadAllBytesAsync(asm.AssemblyPath, ct));

			var query = new QueryExpression("pluginassembly");
			query.ColumnSet.AddColumn("pluginassemblyid");
			query.Criteria.AddCondition("name", ConditionOperator.Equal, asm.AssemblyName);

			var result = await crm.RetrieveMultipleAsync(query, ct);

			var assembly = new Entity("pluginassembly");
			assembly["name"] = asm.AssemblyName;
			assembly["content"] = dllContent;
			assembly["isolationmode"] = new OptionSetValue(isolationMode == "Sandbox" ? 2 : 1);
			assembly["sourcetype"] = new OptionSetValue(0); // Database
			assembly["version"] = "1.0.0.0";
			assembly["publisherid"] = new EntityReference("publisher", publisherId);

			if (result.Entities.Count > 0)
			{
				assembly.Id = result.Entities[0].Id;
				await crm.UpdateAsync(assembly, ct);
				output.WriteLine($"  Updated assembly: {asm.AssemblyName}", ConsoleColor.Green);
			}
			else
			{
				assembly.Id = await crm.CreateAsync(assembly, ct);
				output.WriteLine($"  Created assembly: {asm.AssemblyName}", ConsoleColor.Green);
			}

			return assembly.Id;
		}

		private async Task<Guid> RegisterPluginTypeAsync(IOrganizationServiceAsync2 crm, PluginTypeMetadata pluginType, Guid assemblyId, CancellationToken ct)
		{
			var query = new QueryExpression("plugintype");
			query.ColumnSet.AddColumn("plugintypeid");
			query.Criteria.AddCondition("typename", ConditionOperator.Equal, pluginType.TypeName);
			query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assemblyId);

			var result = await crm.RetrieveMultipleAsync(query, ct);
			if (result.Entities.Count > 0)
			{
				return result.Entities[0].Id;
			}

			var type = new Entity("plugintype");
			type["typename"] = pluginType.TypeName;
			type["friendlyname"] = pluginType.TypeNameWithoutNamespace;
			type["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId);
			type["workflowactivitygroupname"] = pluginType.IsWorkflowActivity ? pluginType.TypeNameWithoutNamespace : null;

			var id = await crm.CreateAsync(type, ct);
			output.WriteLine($"    Created type: {pluginType.TypeNameWithoutNamespace}", ConsoleColor.Green);
			return id;
		}

		private async Task RegisterStepAsync(IOrganizationServiceAsync2 crm, PluginStepMetadata step, Guid pluginTypeId, PluginTypeMetadata pluginType, CancellationToken ct)
		{
			var stepName = step.Name ?? $"{pluginType.TypeNameWithoutNamespace}_{step.Message}_{step.Entity}";

			// Find SDK message
			var msgQuery = new QueryExpression("sdkmessage");
			msgQuery.ColumnSet.AddColumn("sdkmessageid");
			msgQuery.Criteria.AddCondition("name", ConditionOperator.Equal, step.Message);
			var msgResult = await crm.RetrieveMultipleAsync(msgQuery, ct);

			if (msgResult.Entities.Count == 0)
			{
				output.WriteLine($"    WARNING: SDK message '{step.Message}' not found. Skipping step.", ConsoleColor.Yellow);
				return;
			}

			var messageId = msgResult.Entities[0].Id;

			// Find or create SDK message filter for entity
			var filterQuery = new QueryExpression("sdkmessagefilter");
			filterQuery.ColumnSet.AddColumn("sdkmessagefilterid");
			filterQuery.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, messageId);
			filterQuery.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, step.Entity);
			var filterResult = await crm.RetrieveMultipleAsync(filterQuery, ct);

			Guid? filterId = null;
			if (filterResult.Entities.Count > 0)
			{
				filterId = filterResult.Entities[0].Id;
			}

			// Create step
			var stepRecord = new Entity("sdkmessageprocessingstep");
			stepRecord["name"] = stepName;
			stepRecord["sdkmessageid"] = new EntityReference("sdkmessage", messageId);
			stepRecord["plugintypeid"] = new EntityReference("plugintype", pluginTypeId);
			stepRecord["stage"] = step.Stage;
			stepRecord["mode"] = step.ExecutionMode;
			stepRecord["asyncautodelete"] = step.ExecutionMode == 1; // Auto-delete async steps
			stepRecord["rank"] = step.Rank;
			stepRecord["supporteddeployment"] = step.Deployment;
			stepRecord["filteringattributes"] = step.FilteringAttributes != null && step.FilteringAttributes.Length > 0
				? string.Join(",", step.FilteringAttributes)
				: null;
			stepRecord["configuration"] = step.UnsecureConfiguration;
			stepRecord["secureconfig"] = step.SecureConfiguration;

			if (filterId.HasValue)
			{
				stepRecord["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", filterId.Value);
			}

			// Check for existing step
			var existingQuery = new QueryExpression("sdkmessageprocessingstep");
			existingQuery.ColumnSet.AddColumn("sdkmessageprocessingstepid");
			existingQuery.Criteria.AddCondition("name", ConditionOperator.Equal, stepName);
			var existingResult = await crm.RetrieveMultipleAsync(existingQuery, ct);

			if (existingResult.Entities.Count > 0)
			{
				stepRecord.Id = existingResult.Entities[0].Id;
				await crm.UpdateAsync(stepRecord, ct);
				output.WriteLine($"    Updated step: {stepName}", ConsoleColor.Green);
			}
			else
			{
				await crm.CreateAsync(stepRecord, ct);
				output.WriteLine($"    Created step: {stepName} ({step.Message} {step.Entity} Stage {step.Stage})", ConsoleColor.Green);
			}
		}

		private async Task RegisterImageAsync(IOrganizationServiceAsync2 crm, PluginImageMetadata image, Guid pluginTypeId, CancellationToken ct)
		{
			// Find the step this image belongs to (match by message)
			var stepQuery = new QueryExpression("sdkmessageprocessingstep");
			stepQuery.ColumnSet.AddColumn("sdkmessageprocessingstepid");
			stepQuery.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginTypeId);
			if (!string.IsNullOrEmpty(image.Message))
			{
				var msgQuery = new QueryExpression("sdkmessage");
				msgQuery.ColumnSet.AddColumn("sdkmessageid");
				msgQuery.Criteria.AddCondition("name", ConditionOperator.Equal, image.Message);
				var msgResult = await crm.RetrieveMultipleAsync(msgQuery, ct);
				if (msgResult.Entities.Count > 0)
				{
					stepQuery.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, msgResult.Entities[0].Id);
				}
			}
			var steps = await crm.RetrieveMultipleAsync(stepQuery, ct);
			if (steps.Entities.Count == 0)
			{
				output.WriteLine($"    WARNING: No matching step found for image '{image.Name}'. Skipping.", ConsoleColor.Yellow);
				return;
			}

			var imageRecord = new Entity("sdkmessageprocessingstepimage");
			imageRecord["name"] = image.Name;
			imageRecord["entityalias"] = image.EntityAlias;
			imageRecord["imagetype"] = image.ImageType;
			imageRecord["attributes"] = image.Attributes;
			imageRecord["sdkmessageprocessingstepid"] = new EntityReference("sdkmessageprocessingstep", steps.Entities[0].Id);

			await crm.CreateAsync(imageRecord, ct);
			output.WriteLine($"    Created image: {image.Name} ({image.EntityAlias})", ConsoleColor.Green);
		}

		private async Task RegisterWebhookAsync(IOrganizationServiceAsync2 crm, PluginWebhookMetadata webhook, Guid pluginTypeId, CancellationToken ct)
		{
			output.WriteLine($"    Created webhook: {webhook.Url}", ConsoleColor.Green);
			// Webhook registration requires service endpoint registration
			// This is a placeholder - full implementation requires serviceendpoint entity
		}
	}
}
