using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Plugin;
using Microsoft.PowerPlatform.Dataverse.Client;
using Newtonsoft.Json;
using System.Text.Json;

namespace Greg.Xrm.Command.Commands.Plugin
{
	/// <summary>
	/// Executes the plugin list command to retrieve and display plugin steps based on various filtering criteria.
	/// Supports filtering by assembly, plugin type, table, or solution with optimized batch processing for images.
	/// </summary>
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IPluginTypeRepository pluginTypeRepository,
		ISdkMessageProcessingStepRepository sdkMessageProcessingStepRepository,
		ISdkMessageProcessingStepImageRepository sdkMessageProcessingStepImageRepository,
		ISolutionRepository solutionRepository
	) : ICommandExecutor<ListCommand>
	{
		/// <summary>
		/// Main execution method that orchestrates the plugin step retrieval and display process.
		/// Handles connection establishment, step retrieval, processing, and output formatting.
		/// </summary>
		/// <param name="command">The list command containing filter criteria and output options</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>Command result indicating success or failure with appropriate messages</returns>
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			// Establish connection to Dataverse environment
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			// Retrieve plugin steps based on specified filters
			var steps = await RetrieveStepsAsync(crm, command, cancellationToken);
			if (steps == null) // Error occurred, already handled in RetrieveStepsAsync
				return CommandResult.Fail("Failed to retrieve plugin steps.");

			// Handle case when no steps are found
			if (steps.Length == 0)
			{
				output.WriteLine("No plugin steps found.", ConsoleColor.Yellow);
				return CommandResult.Success();
			}

			// Determine whether to show solution membership column
			// Only shown when using solution-based filtering (no other filter parameters specified)
			var showSolutionMembership = string.IsNullOrWhiteSpace(command.AssemblyName) &&
									   string.IsNullOrWhiteSpace(command.PluginTypeName) &&
									   string.IsNullOrWhiteSpace(command.TableName);

			// Process steps and retrieve associated image information
			var stepInfos = await ProcessStepInformationAsync(crm, steps, showSolutionMembership, cancellationToken);

			// Output results in requested format (table or JSON)
			WriteOutput(stepInfos, command.Format, showSolutionMembership);

			return CommandResult.Success();
		}





		/// <summary>
		/// Routes step retrieval to the appropriate method based on the specified filter parameters.
		/// Supports filtering by assembly, plugin type, table, or solution (with default solution fallback).
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="command">Command containing filter criteria</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>Array of plugin steps matching the filter criteria, or null if an error occurred</returns>
		private async Task<SdkMessageProcessingStep[]?> RetrieveStepsAsync(IOrganizationServiceAsync2 crm, ListCommand command, CancellationToken cancellationToken)
		{
			// Filter by assembly name or ID
			if (!string.IsNullOrWhiteSpace(command.AssemblyName))
			{
				return await RetrieveStepsByAssemblyAsync(crm, command.AssemblyName, command.ShowInternalPlugins, cancellationToken);
			}

			// Filter by plugin type name or ID
			if (!string.IsNullOrWhiteSpace(command.PluginTypeName))
			{
				return await RetrieveStepsByPluginTypeAsync(crm, command.PluginTypeName, command.ShowInternalPlugins, cancellationToken);
			}

			// Filter by table name (shows all plugin steps registered for the specified table)
			if (!string.IsNullOrWhiteSpace(command.TableName))
			{
				output.Write($"Retrieving steps for table '{command.TableName}'...");
				var steps = await sdkMessageProcessingStepRepository.GetByTableNameAsync(crm, command.TableName, command.ShowInternalPlugins, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				return steps;
			}

			// Default to solution-based retrieval (either specified solution or default solution)
			return await RetrieveStepsBySolutionAsync(crm, command.SolutionName, command.ShowInternalPlugins, cancellationToken);
		}





		/// <summary>
		/// Retrieves plugin steps from assemblies within a specified solution or the default solution.
		/// Handles solution lookup, validation, and step retrieval with solution component tracking.
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="solutionName">Name of the solution to filter by, or null for default solution</param>
		/// <param name="includeInternalStages">Whether to include internal system plugin stages</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>Array of plugin steps from solution assemblies, or null if an error occurred</returns>
		private async Task<SdkMessageProcessingStep[]?> RetrieveStepsBySolutionAsync(IOrganizationServiceAsync2 crm, string? solutionName, bool includeInternalStages, CancellationToken cancellationToken)
		{
			// Determine which solution to use (specified or default)
			var targetSolutionName = solutionName;
			if (string.IsNullOrWhiteSpace(targetSolutionName))
			{
				// Use the current default solution when no solution is specified
				targetSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (string.IsNullOrWhiteSpace(targetSolutionName))
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return null;
				}
				output.Write($"Using default solution '{targetSolutionName}'...");
			}
			else
			{
				output.Write($"Retrieving steps for solution '{targetSolutionName}'...");
			}

			// Validate solution exists and get solution entity
			var solution = await solutionRepository.GetByUniqueNameAsync(crm, targetSolutionName);
			if (solution == null)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return null;
			}

			output.WriteLine("Done", ConsoleColor.Green);
			output.Write($"Retrieving plugin steps from solution assemblies...");

			// Retrieve all plugin steps from assemblies that are components of the solution
			var steps = await sdkMessageProcessingStepRepository.GetBySolutionAsync(crm, solution.Id, includeInternalStages, cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);
			return steps;
		}





		/// <summary>
		/// Retrieves plugin steps for a specific assembly, supporting both assembly names and GUIDs.
		/// Automatically detects whether the input is a GUID and routes to the appropriate repository method.
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="assemblyName">Assembly name or GUID string</param>
		/// <param name="includeInternalStages">Whether to include internal system plugin stages</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>Array of plugin steps from the specified assembly</returns>
		private async Task<SdkMessageProcessingStep[]?> RetrieveStepsByAssemblyAsync(IOrganizationServiceAsync2 crm, string assemblyName, bool includeInternalStages, CancellationToken cancellationToken)
		{
			// Check if the assembly name is a valid GUID for direct ID-based lookup
			if (Guid.TryParse(assemblyName, out var assemblyGuid))
			{
				output.Write($"Retrieving steps for assembly ID '{assemblyGuid}'...");
				var steps = await sdkMessageProcessingStepRepository.GetByAssemblyIdAsync(crm, assemblyGuid, includeInternalStages, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				return steps;
			}
			else
			{
				// Use name-based lookup for assembly identification
				output.Write($"Retrieving steps for assembly '{assemblyName}'...");
				var steps = await sdkMessageProcessingStepRepository.GetByAssemblyNameAsync(crm, assemblyName, includeInternalStages, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				return steps;
			}
		}





		/// <summary>
		/// Retrieves plugin steps for a specific plugin type, supporting both type names and GUIDs.
		/// Automatically detects whether the input is a GUID and routes to the appropriate retrieval method.
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="pluginTypeName">Plugin type name or GUID string</param>
		/// <param name="includeInternalStages">Whether to include internal system plugin stages</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>Array of plugin steps from the specified plugin type, or null if an error occurred</returns>
		private async Task<SdkMessageProcessingStep[]?> RetrieveStepsByPluginTypeAsync(IOrganizationServiceAsync2 crm, string pluginTypeName, bool includeInternalStages, CancellationToken cancellationToken)
		{
			// Check if the plugin type name is a valid GUID for direct ID-based lookup
			if (Guid.TryParse(pluginTypeName, out var pluginTypeGuid))
			{
				output.Write($"Retrieving steps for plugin type ID '{pluginTypeGuid}'...");
				var steps = await sdkMessageProcessingStepRepository.GetByPluginTypeIdAsync(crm, pluginTypeGuid, includeInternalStages, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);
				return steps;
			}
			else
			{
				// Use name-based fuzzy search for plugin type identification
				return await RetrieveStepsByPluginTypeNameAsync(crm, pluginTypeName, includeInternalStages, cancellationToken);
			}
		}





		/// <summary>
		/// Retrieves plugin steps by plugin type name using fuzzy search.
		/// Handles multiple matches by displaying options and providing guidance to the user.
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="pluginTypeName">Plugin type name for fuzzy search</param>
		/// <param name="includeInternalStages">Whether to include internal system plugin stages</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>Array of plugin steps from the matched plugin type, or null if no unique match found</returns>
		private async Task<SdkMessageProcessingStep[]?> RetrieveStepsByPluginTypeNameAsync(IOrganizationServiceAsync2 crm, string pluginTypeName, bool includeInternalStages, CancellationToken cancellationToken)
		{
			// Perform fuzzy search to find plugin types matching the provided name
			output.Write($"Retrieving plugin type '{pluginTypeName}'...");
			var pluginTypes = await pluginTypeRepository.FuzzySearchAsync(crm, pluginTypeName, cancellationToken);

			// Handle case when no plugin types are found
			if (pluginTypes.Length == 0)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return null;
			}

			// Handle case when multiple plugin types match - require user to be more specific
			if (pluginTypes.Length > 1)
			{
				HandleMultiplePluginTypesFound(pluginTypes);
				return null;
			}

			// Single match found - proceed with step retrieval
			var pluginType = pluginTypes[0];
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write($"Retrieving steps for plugin type '{pluginType.name}'...");
			var steps = await sdkMessageProcessingStepRepository.GetByPluginTypeIdAsync(crm, pluginType.Id, includeInternalStages, cancellationToken);
			output.WriteLine("Done", ConsoleColor.Green);
			return steps;
		}





		/// <summary>
		/// Handles the case when multiple plugin types are found during fuzzy search.
		/// Displays all matching options and provides guidance on how to resolve the ambiguity.
		/// </summary>
		/// <param name="pluginTypes">Array of plugin types that match the search criteria</param>
		private void HandleMultiplePluginTypesFound(PluginType[] pluginTypes)
		{
			output.WriteLine("Failed", ConsoleColor.Red);
			output.WriteLine("Multiple plugin types found matching the provided name. Please be more specific or use the plugin type ID directly. Matching plugin types:", ConsoleColor.Yellow);

			// Display all matching plugin types with their IDs and assembly information
			foreach (var pt in pluginTypes)
			{
				output.WriteLine($"- {pt.name} (Id: {pt.Id}, Assembly: {pt.pluginassemblyid?.Name})", ConsoleColor.Yellow);
			}

			output.WriteLine();
			output.WriteLine("To use a specific plugin type, run:", ConsoleColor.Cyan);
			// Provide example command using the first plugin type's ID
			output.WriteLine($"pacx plugin list --class {pluginTypes[0].Id}", ConsoleColor.White);
		}





		/// <summary>
		/// Processes the retrieved plugin steps to create display-friendly objects with image information.
		/// Uses optimized batch processing to retrieve step images to minimize database calls.
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="steps">Array of plugin steps to process</param>
		/// <param name="showSolutionMembership">Whether to include solution membership information</param>
		/// <param name="cancellationToken">Token to handle cancellation requests</param>
		/// <returns>List of processed plugin step information objects</returns>
		private async Task<List<PluginStepInfo>> ProcessStepInformationAsync(IOrganizationServiceAsync2 crm, SdkMessageProcessingStep[] steps, bool showSolutionMembership, CancellationToken cancellationToken)
		{
			output.Write("Processing step information...");

			// Initialize collection for processed step information
			var stepInfos = new List<PluginStepInfo>();

			// Extract all step IDs for efficient batch processing of images
			var stepIds = steps.Select(s => s.Id).ToArray();

			// Retrieve all step images in batches to optimize database calls
			var imagesByStepId = await GetImagesByStepIdsAsync(crm, stepIds);

			// Process each step and create display-friendly information objects
			foreach (var step in steps)
			{
				var stepInfo = CreatePluginStepInfo(step, imagesByStepId, showSolutionMembership);
				stepInfos.Add(stepInfo);
			}

			output.WriteLine("Done", ConsoleColor.Green);
			return stepInfos;
		}





		/// <summary>
		/// Creates a display-friendly PluginStepInfo object from a raw SdkMessageProcessingStep entity.
		/// Processes image information and formats display values for better user experience.
		/// </summary>
		/// <param name="step">Raw plugin step entity from Dataverse</param>
		/// <param name="imagesByStepId">Dictionary containing images indexed by step ID</param>
		/// <param name="showSolutionMembership">Whether to populate solution membership information</param>
		/// <returns>Formatted plugin step information object</returns>
		private PluginStepInfo CreatePluginStepInfo(SdkMessageProcessingStep step, Dictionary<Guid, SdkMessageProcessingStepImage[]> imagesByStepId, bool showSolutionMembership = false)
		{
			// Retrieve images for this step from the cached dictionary (empty array if none found)
			var stepImages = imagesByStepId.ContainsKey(step.Id) ? imagesByStepId[step.Id] : [];

			return new PluginStepInfo
			{
				StepId = step.Id,
				Name = step.name ?? "Unknown",
				PluginTypeName = step.plugintypename,
				PluginTypeId = step.plugintypeidaliased,
				AssemblyName = step.assemblyname,
				AssemblyId = step.assemblyidaliased,
				Message = step.messagename,
				Table = step.primaryobjecttypecode,
				Stage = PluginStepInfo.GetStageDisplayName(step.stage?.Value),
				Mode = PluginStepInfo.GetModeDisplayName(step.mode?.Value),
				Rank = step.rank ?? 0,
				Status = PluginStepInfo.GetStatusDisplayName(step.statuscode?.Value),
				HasPreImage = stepImages.Any(img => img.imagetype?.Value == 0), // 0 = PreImage
				HasPostImage = stepImages.Any(img => img.imagetype?.Value == 1), // 1 = PostImage
				IsInSolution = showSolutionMembership && step.isstepinsolution
			};
		}





		/// <summary>
		/// Routes output generation to the appropriate method based on the requested format.
		/// Supports both tabular and JSON output formats with conditional column display.
		/// </summary>
		/// <param name="stepInfos">Processed plugin step information to display</param>
		/// <param name="format">Requested output format (Table or JSON)</param>
		/// <param name="showSolutionMembership">Whether to include solution membership column in table format</param>
		private void WriteOutput(List<PluginStepInfo> stepInfos, ListCommand.OutputFormat format, bool showSolutionMembership)
		{
			if (format == ListCommand.OutputFormat.Json)
			{
				WriteJsonOutput(stepInfos);
			}
			else
			{
				WriteTableOutput(stepInfos, showSolutionMembership);
			}
		}






		/// <summary>
		/// Generates JSON output for the plugin step information.
		/// Uses camelCase naming policy for consistency with web standards.
		/// </summary>
		/// <param name="stepInfos">Plugin step information to serialize</param>
		private void WriteJsonOutput(List<PluginStepInfo> stepInfos)
		{
			// Serialize and output the JSON
			var json = JsonConvert.SerializeObject(stepInfos, Formatting.Indented);
			output.WriteLine();
			output.WriteLine(json);
		}





		/// <summary>
		/// Generates tabular output for the plugin step information.
		/// Conditionally includes the "In Solution" column based on solution membership flag.
		/// </summary>
		/// <param name="stepInfos">Plugin step information to display in table format</param>
		/// <param name="showSolutionMembership">Whether to include the "In Solution" column</param>
		private void WriteTableOutput(List<PluginStepInfo> stepInfos, bool showSolutionMembership)
		{
			output.WriteLine();

			if (showSolutionMembership)
			{
				// Table format with solution membership column for solution-based filtering
				output.WriteTable(
					stepInfos,
					rowHeaders: () => ["Assembly", "Plugin Type", "Message", "Table", "Stage", "Mode", "Rank", "Status", "Images", "In Solution"],
					rowData: step =>
					[
						step.AssemblyName,
						step.PluginTypeName,
						step.Message,
						string.IsNullOrEmpty(step.Table) ? "(Global)" : step.Table,
						step.Stage,
						step.Mode,
						step.Rank.ToString(),
						step.Status,
						step.Images,
						step.IsInSolution ? "✓ Yes" : "✕ No"
					]);
			}
			else
			{
				// Standard table format without solution membership column
				output.WriteTable(
					stepInfos,
					rowHeaders: () => ["Assembly", "Plugin Type", "Message", "Table", "Stage", "Mode", "Rank", "Status", "Images"],
					rowData: step =>
					[
						step.AssemblyName,
						step.PluginTypeName,
						step.Message,
						string.IsNullOrEmpty(step.Table) ? "(Global)" : step.Table,
						step.Stage,
						step.Mode,
						step.Rank.ToString(),
						step.Status,
						step.Images
					]);
			}

			output.WriteLine();
			output.WriteLine($"Total: {stepInfos.Count} plugin step(s) found", ConsoleColor.Green);
		}






		/// <summary>
		/// Efficiently retrieves plugin step images for multiple steps using batched queries.
		/// Implements batch processing with a 100-item limit per query to optimize database performance
		/// and avoid hitting Dataverse IN operator limitations.
		/// </summary>
		/// <param name="crm">Dataverse service client for database operations</param>
		/// <param name="stepIds">Array of step IDs to retrieve images for</param>
		/// <returns>Dictionary mapping step IDs to their associated images</returns>
		private async Task<Dictionary<Guid, SdkMessageProcessingStepImage[]>> GetImagesByStepIdsAsync(IOrganizationServiceAsync2 crm, Guid[] stepIds)
		{
			const int batchSize = 100; // IN operator limit in Dataverse queries
			var imagesByStepId = new Dictionary<Guid, SdkMessageProcessingStepImage[]>();

			// Return empty dictionary if no step IDs provided
			if (stepIds.Length == 0)
				return imagesByStepId;

			// Process step IDs in batches to avoid exceeding IN operator limits
			for (int i = 0; i < stepIds.Length; i += batchSize)
			{
				// Extract current batch of step IDs
				var batch = stepIds.Skip(i).Take(batchSize).ToArray();

				// Retrieve images for current batch
				var batchImages = await sdkMessageProcessingStepImageRepository.GetByStepIdsAsync(crm, batch);

				// Group images by step ID for efficient lookup
				var batchImagesByStepId = batchImages
					.Where(img => img.sdkmessageprocessingstepid?.Id != null)
					.GroupBy(img => img.sdkmessageprocessingstepid!.Id)
					.ToDictionary(g => g.Key, g => g.ToArray());

				// Merge batch results into main dictionary
				foreach (var kvp in batchImagesByStepId)
				{
					imagesByStepId[kvp.Key] = kvp.Value;
				}
			}

			return imagesByStepId;
		}
	}
}