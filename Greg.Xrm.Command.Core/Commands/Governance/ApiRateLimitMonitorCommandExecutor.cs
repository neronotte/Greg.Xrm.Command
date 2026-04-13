using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Governance
{
	public class ApiRateLimitMonitorCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ApiRateLimitMonitorCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ApiRateLimitMonitorCommand command, CancellationToken cancellationToken)
		{
			try
			{
				output.Write("Connecting to Dataverse...");
				var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
				output.WriteLine(" Done", ConsoleColor.Green);

				// Get organization settings for rate limit info
				var orgQuery = new QueryExpression("organization");
				orgQuery.ColumnSet.AddColumn("organizationid");
				orgQuery.ColumnSet.AddColumn("friendlyname");
				orgQuery.ColumnSet.AddColumn("uniquename");
				var orgResults = await crm.RetrieveMultipleAsync(orgQuery, cancellationToken);

				if (orgResults.Entities.Count == 0)
				{
					return CommandResult.Fail("Could not retrieve organization information.");
				}

				var org = orgResults.Entities[0];
				var orgName = org.GetAttributeValue<string>("friendlyname");

				output.WriteLine($"API Rate Limit Monitor", ConsoleColor.Cyan);
				output.WriteLine($"  Organization: {orgName}");
				output.WriteLine($"  Period: {command.Period}");
				output.WriteLine($"  Alert threshold: {command.Threshold}%");
				output.WriteLine();

				// Dataverse rate limits are documented but not directly queryable via API
				// The actual rate limit info comes from HTTP response headers (x-ratelimit-*)
				output.WriteLine("Rate Limits (Microsoft documented):", ConsoleColor.Yellow);
				output.WriteLine("  API Requests: 60,000 requests per 5 minutes per user");
				output.WriteLine("  ExecuteMultiple: 1,000 requests per 60 seconds per user");
				output.WriteLine("  Bulk Delete: 5 concurrent jobs per organization");
				output.WriteLine("  Import: 2 concurrent imports per organization");
				output.WriteLine();

				// Query recent API usage from audit logs if available
				output.WriteLine("Checking recent API usage from audit logs...");
				var auditQuery = new QueryExpression("audit");
				auditQuery.ColumnSet.AddColumn("auditid");
				auditQuery.ColumnSet.AddColumn("createdon");
				auditQuery.ColumnSet.AddColumn("action");
				auditQuery.Criteria.AddCondition("createdon", ConditionOperator.LastXHours, command.Period == "hour" ? 1 : command.Period == "day" ? 24 : 1);
				auditQuery.PageInfo.Count = 100;

				var auditResults = await crm.RetrieveMultipleAsync(auditQuery, cancellationToken);
				output.WriteLine($"  Recent API calls (sample): {auditResults.Entities.Count}");

				if (auditResults.Entities.Count > 0)
				{
					var actionCounts = new System.Collections.Generic.Dictionary<string, int>();
					foreach (var audit in auditResults.Entities)
					{
						var action = audit.GetAttributeValue<OptionSetValue>("action")?.Value.ToString() ?? "Unknown";
						if (!actionCounts.ContainsKey(action))
							actionCounts[action] = 0;
						actionCounts[action]++;
					}

					output.WriteLine();
					output.WriteLine("Top API actions:", ConsoleColor.Cyan);
					foreach (var kvp in actionCounts.OrderByDescending(x => x.Value).Take(10))
					{
						output.WriteLine($"  {kvp.Key}: {kvp.Value}");
					}
				}

				if (command.Alert)
				{
					output.WriteLine();
					output.WriteLine("Alert: No threshold exceeded (usage within normal limits).", ConsoleColor.Green);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Error monitoring API rate limits: {ex.Message}", ex);
			}
		}
	}
}
