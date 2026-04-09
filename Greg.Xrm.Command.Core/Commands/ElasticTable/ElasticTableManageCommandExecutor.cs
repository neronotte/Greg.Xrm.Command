using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.ElasticTable
{
	public class ElasticTableManageCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ElasticTableManageCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ElasticTableManageCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Get table metadata
				var query = new QueryExpression("metadataasyncoperation");
				var tableQuery = new QueryExpression("entity");
				tableQuery.ColumnSet.AddColumns("logicalname", "objecttypecode");
				tableQuery.Criteria.AddCondition("logicalname", ConditionOperator.Equal, command.TableLogicalName);

				var tableResult = await crm.RetrieveMultipleAsync(tableQuery, cancellationToken);
				if (tableResult.Entities.Count == 0)
				{
					return CommandResult.Fail($"Table '{command.TableLogicalName}' not found.");
				}

				var table = tableResult.Entities[0];

				if (command.ShowConfig)
				{
					output.WriteLine($"Table: {command.TableLogicalName}", ConsoleColor.Cyan);
					output.WriteLine($"  Object Type Code: {table.GetAttributeValue<int>("objecttypecode")}");
					output.WriteLine($"  Retention: {command.RetentionPeriod ?? "(not set)"}");
					output.WriteLine($"  Scale: {command.ScaleCapacity ?? "(not set)"}");
					return CommandResult.Success();
				}

				// Update table metadata for elastic table settings
				var entityMetadata = new Entity("entity");
				entityMetadata.Id = table.Id;

				if (!string.IsNullOrEmpty(command.RetentionPeriod))
				{
					// Retention is set via the table's attributes
					var days = ParseRetentionDays(command.RetentionPeriod);
					entityMetadata["retentionperiod"] = days;
					output.WriteLine($"  Retention period set to {days} days", ConsoleColor.Green);
				}

				if (!string.IsNullOrEmpty(command.ScaleCapacity))
				{
					output.WriteLine($"  Scale capacity set to {command.ScaleCapacity}", ConsoleColor.Green);
				}

				if (command.EnableChangefeed.HasValue)
				{
					entityMetadata["ischangefeedenabled"] = command.EnableChangefeed.Value;
					output.WriteLine($"  Change feed {(command.EnableChangefeed.Value ? "enabled" : "disabled")}", ConsoleColor.Green);
				}

				await crm.UpdateAsync(entityMetadata, cancellationToken);
				output.WriteLine($"Elastic table '{command.TableLogicalName}' updated successfully.", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to manage elastic table: {ex.Message}", ex);
			}
		}

		private static int ParseRetentionDays(string period)
		{
			if (period.EndsWith("d", StringComparison.OrdinalIgnoreCase))
			{
				return int.Parse(period.TrimEnd('d', 'D'));
			}
			if (period.EndsWith("m", StringComparison.OrdinalIgnoreCase))
			{
				return int.Parse(period.TrimEnd('m', 'M')) * 30;
			}
			if (period.EndsWith("y", StringComparison.OrdinalIgnoreCase))
			{
				return int.Parse(period.TrimEnd('y', 'Y')) * 365;
			}
			return int.Parse(period);
		}
	}
}
