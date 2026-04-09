using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Plugin.Automation.Commands.Workflow
{
	public class ConnectionListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ConnectionListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectionListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			output.Write("Retrieving connections...");

			var query = new QueryExpression("connection");
			query.ColumnSet = new ColumnSet("name", "statecode", "statuscode", "connectionid");
			// We might want to join with connector info if available in Dataverse
			
			try
			{
				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				if (result.Entities.Count == 0)
				{
					output.WriteLine("No connections found.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				output.WriteTable(result.Entities.ToList(),
					() => new[] { "Name", "Status", "ID" },
					conn => new[] {
						conn.GetAttributeValue<string>("name") ?? "-",
						conn.GetFormattedValue("statuscode") ?? "-",
						conn.Id.ToString()
					}
				);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail("Error while retrieving connections: " + ex.Message, ex);
			}
		}
	}
}
