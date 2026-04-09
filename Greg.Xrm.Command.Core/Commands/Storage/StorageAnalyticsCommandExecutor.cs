using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Storage
{
	public class StorageAnalyticsCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<StorageAnalyticsCommand>
	{
		public async Task<CommandResult> ExecuteAsync(StorageAnalyticsCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Query entity metadata for storage info
				var query = new QueryExpression("entity");
				query.ColumnSet.AddColumns("logicalname", "displayname", "objecttypecode");
				query.TopCount = command.TopN;

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				output.WriteLine($"Storage Analytics — Top {command.TopN} Tables", ConsoleColor.Cyan);
				output.WriteLine();

				if (result.Entities.Count > 0)
				{
					output.WriteTable(result.Entities,
						() => new[] { "Table", "Display Name", "Object Type Code" },
						e => new[] {
							e.GetAttributeValue<string>("logicalname") ?? "-",
							e.GetAttributeValue<string>("displayname") ?? "-",
							e.GetAttributeValue<int?>("objecttypecode")?.ToString() ?? "-"
						}
					);

					if (command.IncludeRecommendations)
					{
						output.WriteLine();
						output.WriteLine("Recommendations:", ConsoleColor.Yellow);
						output.WriteLine("  - Review audit log tables for old entries");
						output.WriteLine("  - Archive inactive records");
						output.WriteLine("  - Consider Elastic Tables for high-volume data");
						output.WriteLine("  - Remove unused custom entities");
					}
				}
				else
				{
					output.WriteLine("No tables found.", ConsoleColor.Yellow);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Storage analytics error: {ex.Message}", ex);
			}
		}
	}
}
