using Greg.Xrm.Command.Commands.WebResources.ProjectFile;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.WebResources
{
	public class WebResourceMapCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IWebResourceRepository webResourceRepository,
		ISolutionRepository solutionRepository) : ICommandExecutor<WebResourceMapCommand>
	{
		public async Task<CommandResult> ExecuteAsync(WebResourceMapCommand command, CancellationToken cancellationToken)
		{
			try
			{
				// 1. Parse config file
				if (!File.Exists(command.ConfigPath))
				{
					return CommandResult.Fail($"Config file not found: {command.ConfigPath}");
				}

				var config = ParseConfig(command.ConfigPath);
				if (config == null || config.Mappings.Count == 0)
				{
					return CommandResult.Fail("No mappings found in config file.");
				}

				output.WriteLine($"Loaded {config.Mappings.Count} mapping(s) from {Path.GetFileName(command.ConfigPath)}", ConsoleColor.Cyan);

				// 2. Validate local files exist
				var missingFiles = config.Mappings
					.Where(m => !File.Exists(m.LocalPath))
					.Select(m => m.LocalPath)
					.ToList();

				if (missingFiles.Count > 0)
				{
					output.WriteLine($"Warning: {missingFiles.Count} local file(s) not found:", ConsoleColor.Yellow);
					foreach (var file in missingFiles)
					{
						output.WriteLine($"  - {file}");
					}
				}

				// 3. Dry run
				if (command.DryRun)
				{
					output.WriteLine();
					output.WriteLine("[DRY RUN] Would map the following files:", ConsoleColor.Yellow);
					foreach (var mapping in config.Mappings)
					{
						output.WriteLine($"  {mapping.LocalPath} -> {mapping.UniqueName} ({GetWebResourceType(mapping.UniqueName)})");
					}
					return CommandResult.Success();
				}

				// 4. Connect to Dataverse
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// 5. Resolve solution
				var solutionId = Guid.Empty;
				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
				{
					var solution = await solutionRepository.GetByNameAsync(command.SolutionUniqueName, cancellationToken);
					if (solution == null)
					{
						return CommandResult.Fail($"Solution '{command.SolutionUniqueName}' not found.");
					}
					solutionId = solution.Id;
					output.WriteLine($"Target solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);
				}

				// 6. Upload web resources
				var uploaded = 0;
				var updated = 0;
				var errors = 0;

				foreach (var mapping in config.Mappings)
				{
					if (!File.Exists(mapping.LocalPath))
					{
						output.WriteLine($"  SKIP: {mapping.LocalPath} (file not found)", ConsoleColor.Yellow);
						continue;
					}

					var result = await UploadWebResourceAsync(crm, mapping, solutionId, command.Force, cancellationToken);
					if (result == UploadResult.Created) uploaded++;
					else if (result == UploadResult.Updated) updated++;
					else errors++;
				}

				output.WriteLine();
				output.WriteLine($"Web resource mapping complete:", ConsoleColor.Green);
				output.WriteLine($"  Created: {uploaded}");
				output.WriteLine($"  Updated: {updated}");
				if (errors > 0)
				{
					output.WriteLine($"  Errors: {errors}", ConsoleColor.Red);
				}

				// 7. Publish
				if (command.Publish && (uploaded + updated) > 0)
				{
					output.Write("Publishing web resources...");
					var publishRequest = new PublishAllXmlRequest();
					await crm.ExecuteAsync(publishRequest, cancellationToken);
					output.WriteLine(" Done", ConsoleColor.Green);
				}

				return errors > 0 ? CommandResult.Fail($"{errors} error(s) during upload.") : CommandResult.Success();
			}
			catch (Exception ex) when (ex is IOException or InvalidOperationException)
			{
				return CommandResult.Fail($"Error during web resource mapping: {ex.Message}", ex);
			}
		}

		private WebResourceMapConfig? ParseConfig(string configPath)
		{
			var ext = Path.GetExtension(configPath).ToLowerInvariant();
			var content = File.ReadAllText(configPath);

			if (ext is ".json")
			{
				return JsonSerializer.Deserialize<WebResourceMapConfig>(content);
			}

			// For YAML, we'd need YamlDotNet — for now, support JSON only
			// and treat YAML as a future enhancement
			return JsonSerializer.Deserialize<WebResourceMapConfig>(content);
		}

		private async Task<UploadResult> UploadWebResourceAsync(IOrganizationServiceAsync2 crm, WebResourceMapping mapping, Guid solutionId, bool force, CancellationToken ct)
		{
			var content = await File.ReadAllBytesAsync(mapping.LocalPath, ct);
			var base64Content = Convert.ToBase64String(content);
			var webResourceType = GetWebResourceTypeNumber(mapping.UniqueName);

			// Check if web resource already exists
			var query = new QueryExpression("webresource");
			query.ColumnSet.AddColumn("webresourceid");
			query.ColumnSet.AddColumn("content");
			query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, mapping.UniqueName);

			var result = await crm.RetrieveMultipleAsync(query, ct);

			var wr = new Entity("webresource");
			wr["uniquename"] = mapping.UniqueName;
			wr["name"] = mapping.DisplayName ?? mapping.UniqueName;
			wr["webresourcetype"] = new OptionSetValue(webResourceType);
			wr["content"] = base64Content;

			if (solutionId != Guid.Empty)
			{
				wr["solutionid"] = new EntityReference("solution", solutionId);
			}

			if (result.Entities.Count > 0)
			{
				var existingId = result.Entities[0].Id;
				var existingContent = result.Entities[0].GetAttributeValue<string>("content");

				if (existingContent == base64Content)
				{
					output.WriteLine($"  UP-TO-DATE: {mapping.UniqueName}", ConsoleColor.DarkGray);
					return UploadResult.Updated;
				}

				if (!force)
				{
					output.WriteLine($"  SKIP: {mapping.UniqueName} (already exists, use --force to overwrite)", ConsoleColor.Yellow);
					return UploadResult.Skipped;
				}

				wr.Id = existingId;
				await crm.UpdateAsync(wr, ct);
				output.WriteLine($"  UPDATED: {mapping.UniqueName} <- {mapping.LocalPath}", ConsoleColor.Green);
				return UploadResult.Updated;
			}

			wr.Id = await crm.CreateAsync(wr, ct);
			output.WriteLine($"  CREATED: {mapping.UniqueName} <- {mapping.LocalPath}", ConsoleColor.Green);
			return UploadResult.Created;
		}

		private static int GetWebResourceTypeNumber(string uniqueName)
		{
			var ext = Path.GetExtension(uniqueName).ToLowerInvariant();
			return ext switch
			{
				".html" or ".htm" => 1,
				".css" => 2,
				".js" => 3,
				".xml" => 4,
				".png" => 5,
				".jpg" or ".jpeg" => 6,
				".gif" => 7,
				".xap" => 8,
				".xsl" => 9,
				".ico" => 10,
				".svg" => 11,
				".resx" => 12,
				_ => 3, // Default to script
			};
		}

		private static string GetWebResourceType(string uniqueName)
		{
			var ext = Path.GetExtension(uniqueName).ToLowerInvariant();
			return ext switch
			{
				".html" or ".htm" => "Webpage (HTML)",
				".css" => "Stylesheet (CSS)",
				".js" => "Script (JS)",
				".png" => "PNG",
				".jpg" or ".jpeg" => "JPG",
				".gif" => "GIF",
				".svg" => "SVG",
				".ico" => "ICO",
				".xml" => "XML",
				".xsl" => "XSL",
				_ => "Script (JS)",
			};
		}

		private enum UploadResult { Created, Updated, Skipped, Error }
	}

	public class WebResourceMapConfig
	{
		[JsonPropertyName("mappings")]
		public List<WebResourceMapping> Mappings { get; set; } = new();
	}

	public class WebResourceMapping
	{
		[JsonPropertyName("localPath")]
		public string LocalPath { get; set; } = "";

		[JsonPropertyName("uniqueName")]
		public string UniqueName { get; set; } = "";

		[JsonPropertyName("displayName")]
		public string? DisplayName { get; set; }
	}
}
