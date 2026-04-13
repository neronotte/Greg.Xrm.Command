using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Env
{
	public class EnvResetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<EnvResetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(EnvResetCommand command, CancellationToken cancellationToken)
		{
			try
			{
				if (!command.Force)
				{
					output.WriteLine($"WARNING: This will reset environment '{command.EnvironmentId}' with type '{command.ResetType}'.", ConsoleColor.Red);
					output.WriteLine("This operation cannot be undone. Use --force to skip this warning.", ConsoleColor.Red);
					return CommandResult.Fail("Reset aborted. Use --force to confirm.");
				}

				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Verify environment exists
				var query = new QueryExpression("environment");
				query.ColumnSet.AddColumn("environmentid");
				query.ColumnSet.AddColumn("friendlyname");
				query.Criteria.AddCondition("environmentid", ConditionOperator.Equal, command.EnvironmentId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Environment '{command.EnvironmentId}' not found.");
				}

				var envName = results.Entities[0].GetAttributeValue<string>("friendlyname");
				output.WriteLine($"Resetting environment: {envName} ({command.EnvironmentId})", ConsoleColor.Cyan);
				output.WriteLine($"  Reset type: {command.ResetType}");

				// Environment reset is performed via Power Platform Admin API
				// POST /providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{envId}/reset
				// This is a long-running operation
				output.WriteLine();
				output.WriteLine("Note: Environment reset requires Power Platform Admin API access.", ConsoleColor.Yellow);
				output.WriteLine("Use the Power Platform Admin Center or call the Admin API directly:");
				output.WriteLine($"  POST https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{command.EnvironmentId}/reset");
				output.WriteLine($"  Body: {{ \"resetType\": \"{command.ResetType}\" }}");

				if (command.Wait)
				{
					output.WriteLine("Use the Power Platform Admin Center to monitor reset progress.");
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error resetting environment: {ex.Message}", ex);
			}
		}
	}

	public class EnvBackupCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<EnvBackupCommand>
	{
		public async Task<CommandResult> ExecuteAsync(EnvBackupCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Verify environment exists
				var query = new QueryExpression("environment");
				query.ColumnSet.AddColumn("environmentid");
				query.ColumnSet.AddColumn("friendlyname");
				query.Criteria.AddCondition("environmentid", ConditionOperator.Equal, command.EnvironmentId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Environment '{command.EnvironmentId}' not found.");
				}

				var envName = results.Entities[0].GetAttributeValue<string>("friendlyname");
				var backupName = command.BackupName ?? $"backup-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}";
				var mode = command.IncludeData ? "schema+data" : "schema-only";

				output.WriteLine($"Backing up environment: {envName} ({command.EnvironmentId})", ConsoleColor.Cyan);
				output.WriteLine($"  Backup name: {backupName}");
				output.WriteLine($"  Mode: {mode}");

				// Environment backup is performed via Power Platform Admin API
				output.WriteLine();
				output.WriteLine("Note: Environment backup requires Power Platform Admin API access.", ConsoleColor.Yellow);
				output.WriteLine("Use the Power Platform Admin Center or call the Admin API directly:");
				output.WriteLine($"  POST https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{command.EnvironmentId}/backups");
				output.WriteLine($"  Body: {{ \"backupName\": \"{backupName}\", \"mode\": \"{mode}\" }}");

				if (command.Wait)
				{
					output.WriteLine("Use the Power Platform Admin Center to monitor backup progress.");
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error backing up environment: {ex.Message}", ex);
			}
		}
	}

	public class EnvRestoreCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<EnvRestoreCommand>
	{
		public async Task<CommandResult> ExecuteAsync(EnvRestoreCommand command, CancellationToken cancellationToken)
		{
			try
			{
				if (!command.Force)
				{
					output.WriteLine($"WARNING: This will restore environment '{command.EnvironmentId}' from backup '{command.BackupId}'.", ConsoleColor.Red);
					output.WriteLine("This operation cannot be undone. Use --force to skip this warning.", ConsoleColor.Red);
					return CommandResult.Fail("Restore aborted. Use --force to confirm.");
				}

				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Verify environment exists
				var query = new QueryExpression("environment");
				query.ColumnSet.AddColumn("environmentid");
				query.ColumnSet.AddColumn("friendlyname");
				query.Criteria.AddCondition("environmentid", ConditionOperator.Equal, command.EnvironmentId);

				var results = await crm.RetrieveMultipleAsync(query, cancellationToken);
				if (results.Entities.Count == 0)
				{
					return CommandResult.Fail($"Environment '{command.EnvironmentId}' not found.");
				}

				var envName = results.Entities[0].GetAttributeValue<string>("friendlyname");
				output.WriteLine($"Restoring environment: {envName} ({command.EnvironmentId})", ConsoleColor.Cyan);
				output.WriteLine($"  From backup: {command.BackupId}");

				// Environment restore is performed via Power Platform Admin API
				output.WriteLine();
				output.WriteLine("Note: Environment restore requires Power Platform Admin API access.", ConsoleColor.Yellow);
				output.WriteLine("Use the Power Platform Admin Center or call the Admin API directly:");
				output.WriteLine($"  POST https://api.bap.microsoft.com/providers/Microsoft.BusinessAppPlatform/scopes/admin/environments/{command.EnvironmentId}/restore");
				output.WriteLine($"  Body: {{ \"backupId\": \"{command.BackupId}\" }}");

				if (command.Wait)
				{
					output.WriteLine("Use the Power Platform Admin Center to monitor restore progress.");
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error restoring environment: {ex.Message}", ex);
			}
		}
	}
}
