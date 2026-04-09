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

namespace Greg.Xrm.Command.Commands.VirtualTable
{
	public class VirtualTableScaffoldCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<VirtualTableScaffoldCommand>
	{
		public async Task<CommandResult> ExecuteAsync(VirtualTableScaffoldCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			var prefix = command.Prefix ?? command.DataSourceType.ToLowerInvariant().Replace(" ", "_");
			var tables = command.ExternalTables ?? Array.Empty<string>();

			if (command.DryRun)
			{
				output.WriteLine("[DRY RUN] Would scaffold:", ConsoleColor.Yellow);
				output.WriteLine($"  Data source: {command.DataSourceType}");
				output.WriteLine($"  Tables: {string.Join(", ", tables.Any() ? tables : new[] { "(all)" })}");
				output.WriteLine($"  Prefix: {prefix}");
				return CommandResult.Success();
			}

			try
			{
				// Create external data source
				var dataSource = new Entity("externaldatasource");
				dataSource["name"] = $"{prefix}_datasource";
				dataSource["displayname"] = $"{command.DataSourceType} Data Source";

				switch (command.DataSourceType.ToLowerInvariant())
				{
					case "sqlserver":
						dataSource["datasourcetype"] = new OptionSetValue(1);
						break;
					case "odata":
						dataSource["datasourcetype"] = new OptionSetValue(2);
						break;
					case "sharepoint":
						dataSource["datasourcetype"] = new OptionSetValue(3);
						break;
					default:
						return CommandResult.Fail($"Unsupported data source type: {command.DataSourceType}");
				}

				dataSource["connectionstring"] = command.ConnectionString;

				output.Write("Creating external data source...");
				var dataSourceId = await crm.CreateAsync(dataSource, cancellationToken);
				output.WriteLine(" Done", ConsoleColor.Green);

				// Scaffold virtual tables
				foreach (var tableName in tables)
				{
					var virtualTable = new Entity("entity");
					virtualTable["logicalname"] = $"{prefix}_{tableName.ToLowerInvariant()}";
					virtualTable["displayname"] = tableName;
					virtualTable["isactivity"] = false;
					virtualTable["isvalidforadvancedfind"] = true;
					virtualTable["externalname"] = tableName;
					virtualTable["externalsourcename"] = $"{prefix}_datasource";
					virtualTable["dataversion"] = "9.0.0.0";

					await crm.CreateAsync(virtualTable, cancellationToken);
					output.WriteLine($"  Created virtual table: {prefix}_{tableName.ToLowerInvariant()}", ConsoleColor.Green);
				}

				output.WriteLine($"\nScaffolded {tables.Length} virtual table(s) from {command.DataSourceType}.", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to scaffold virtual tables: {ex.Message}", ex);
			}
		}
	}
}
