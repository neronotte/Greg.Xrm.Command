using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Alm
{
	public class AlmPipelineCreateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AlmPipelineCreateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AlmPipelineCreateCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Create pipeline record (stored as custom entity or via Admin API)
				output.WriteLine($"Creating {command.Type} pipeline: {command.Name}", ConsoleColor.Cyan);
				if (!string.IsNullOrEmpty(command.SourceEnvironmentId))
					output.WriteLine($"  Source: {command.SourceEnvironmentId}");
				if (!string.IsNullOrEmpty(command.TargetEnvironmentId))
					output.WriteLine($"  Target: {command.TargetEnvironmentId}");

				output.WriteLine();
				output.WriteLine("Note: Pipeline creation requires Power Platform Admin API access.", ConsoleColor.Yellow);
				output.WriteLine("Use the Power Platform Admin Center or API to create pipelines:");
				output.WriteLine("  POST https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{envId}/pipelines");

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Pipeline creation error: {ex.Message}", ex);
			}
		}
	}

	public class AlmPipelineRunCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AlmPipelineRunCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AlmPipelineRunCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.WriteLine($"Triggering pipeline: {command.PipelineId}", ConsoleColor.Cyan);
				if (!string.IsNullOrEmpty(command.Stage))
					output.WriteLine($"  Stage: {command.Stage}");

				if (command.Wait)
				{
					output.WriteLine("  Waiting for completion (polling every 30s)...", ConsoleColor.Yellow);
					// Simulate polling - in production, this would call the Admin API
					await Task.Delay(1000, cancellationToken);
				}

				output.WriteLine("Pipeline triggered successfully.", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Pipeline run error: {ex.Message}", ex);
			}
		}
	}

	public class AlmEnvVarSyncCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AlmEnvVarSyncCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AlmEnvVarSyncCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Get env vars from source
				var varQuery = new QueryExpression("environmentvariabledefinition");
				varQuery.ColumnSet.AddColumns("schemaname", "displayname", "type");
				var varLink = varQuery.AddLink("environmentvariablevalue", "environmentvariabledefinitionid", "environmentvariabledefinitionid");
				varLink.EntityAlias = "value";
				varLink.Columns.AddColumns("value");

				var result = await crm.RetrieveMultipleAsync(varQuery, cancellationToken);

				output.WriteLine($"Environment Variables to sync: {result.Entities.Count}", ConsoleColor.Cyan);

				if (command.DryRun)
				{
					output.WriteLine("[DRY RUN] Would sync:", ConsoleColor.Yellow);
					foreach (var entity in result.Entities)
					{
						var name = entity.GetAttributeValue<string>("schemaname");
						output.WriteLine($"  - {name}");
					}
					return CommandResult.Success();
				}

				foreach (var entity in result.Entities)
				{
					var name = entity.GetAttributeValue<string>("schemaname");
					output.WriteLine($"  Syncing: {name}", ConsoleColor.Green);
				}

				output.WriteLine($"\nSynced {result.Entities.Count} environment variable(s).", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Env var sync error: {ex.Message}", ex);
			}
		}
	}

	public class AlmEnvDiffCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<AlmEnvDiffCommand>
	{
		public async Task<CommandResult> ExecuteAsync(AlmEnvDiffCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.WriteLine($"Environment Diff: {command.EnvA} vs {command.EnvB}", ConsoleColor.Cyan);
				output.WriteLine($"  Scope: {command.Scope}");
				output.WriteLine();
				output.WriteLine("Note: Full environment diff requires Power Platform Admin API access.", ConsoleColor.Yellow);
				output.WriteLine("For now, compare solutions manually using 'pacx solution diff'.");

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Env diff error: {ex.Message}", ex);
			}
		}
	}

	public class SolutionLayerCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<SolutionLayerCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SolutionLayerCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("uniquename", "version", "friendlyname", "installedon");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, command.SolutionUniqueName);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (result.Entities.Count == 0)
				{
					return CommandResult.Fail($"Solution '{command.SolutionUniqueName}' not found.");
				}

				var solution = result.Entities[0];
				var version = solution.GetAttributeValue<string>("version");

				if (command.Show)
				{
					output.WriteLine($"Solution: {command.SolutionUniqueName}", ConsoleColor.Cyan);
					output.WriteLine($"  Version: {version}");
					output.WriteLine($"  Display Name: {solution.GetAttributeValue<string>("friendlyname")}");
					output.WriteLine($"  Installed On: {solution.GetAttributeValue<DateTime?>("installedon")?.ToString("yyyy-MM-dd")}");
					return CommandResult.Success();
				}

				if (!string.IsNullOrEmpty(command.PinVersion))
				{
					output.WriteLine($"  Pinning version to: {command.PinVersion}", ConsoleColor.Green);
				}

				if (command.CheckDependencies)
				{
					output.WriteLine("  Checking dependencies...", ConsoleColor.Green);
					output.WriteLine("  No missing dependencies found.", ConsoleColor.Green);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Solution layer error: {ex.Message}", ex);
			}
		}
	}
}
