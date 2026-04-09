using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.ConnectionRef
{
	public class ConnectionRefMapCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ConnectionRefMapCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ConnectionRefMapCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var query = new QueryExpression("connectionreference");
				query.ColumnSet.AddColumns("connectionreferencelogicalname", "connectorid", "connectionid", "createdon");
				query.AddOrder("connectionreferencelogicalname", OrderType.Ascending);

				if (!string.IsNullOrEmpty(command.SolutionUniqueName))
				{
					query.Criteria.AddCondition("solutionidname", ConditionOperator.Equal, command.SolutionUniqueName);
				}
				if (!string.IsNullOrEmpty(command.ConnectorId))
				{
					query.Criteria.AddCondition("connectorid", ConditionOperator.Equal, command.ConnectorId);
				}

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);

				if (result.Entities.Count == 0)
				{
					output.WriteLine("No connection references found.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}

				if (command.Format == "json")
				{
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(
						result.Entities.Select(e => new
						{
							Name = e.GetAttributeValue<string>("connectionreferencelogicalname"),
							ConnectorId = e.GetAttributeValue<string>("connectorid"),
							ConnectionId = e.GetAttributeValue<string>("connectionid"),
							CreatedOn = e.GetAttributeValue<DateTime?>("createdon")
						}).ToList(),
						Newtonsoft.Json.Formatting.Indented);
					output.WriteLine(json);
				}
				else
				{
					output.WriteTable(result.Entities,
						() => new[] { "Name", "Connector ID", "Connection ID", "Created" },
						e => new[] {
							e.GetAttributeValue<string>("connectionreferencelogicalname") ?? "-",
							e.GetAttributeValue<string>("connectorid") ?? "-",
							e.GetAttributeValue<string>("connectionid") ?? "-",
							e.GetAttributeValue<DateTime?>("createdon")?.ToString("yyyy-MM-dd") ?? "-"
						}
					);
				}

				output.WriteLine($"\nTotal: {result.Entities.Count} connection reference(s)", ConsoleColor.Cyan);
				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Failed to map connection references: {ex.Message}", ex);
			}
		}
	}
}
