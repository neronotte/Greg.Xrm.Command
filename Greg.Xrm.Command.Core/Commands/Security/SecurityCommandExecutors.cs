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

namespace Greg.Xrm.Command.Commands.Security
{
	public class SecurityAuditUserCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<SecurityAuditUserCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SecurityAuditUserCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// Find user
				var userQuery = new QueryExpression("systemuser");
				userQuery.ColumnSet.AddColumns("systemuserid", "fullname", "domainname", "internalemailaddress");
				userQuery.Criteria.AddCondition("internalemailaddress", ConditionOperator.Equal, command.UserIdentifier);
				var userResult = await crm.RetrieveMultipleAsync(userQuery, cancellationToken);

				if (userResult.Entities.Count == 0)
				{
					return CommandResult.Fail($"User '{command.UserIdentifier}' not found.");
				}

				var user = userResult.Entities[0];
				var userId = user.Id;
				var fullName = user.GetAttributeValue<string>("fullname");

				output.WriteLine($"Security Audit for: {fullName} ({command.UserIdentifier})", ConsoleColor.Cyan);
				output.WriteLine();

				// Get user's roles
				var roleQuery = new QueryExpression("role");
				roleQuery.ColumnSet.AddColumn("name");
				var link = roleQuery.AddLink("systemuserroles", "roleid", "roleid");
				link.LinkCriteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);

				var roles = await crm.RetrieveMultipleAsync(roleQuery, cancellationToken);

				output.WriteLine($"Security Roles ({roles.Entities.Count}):", ConsoleColor.Yellow);
				foreach (var role in roles.Entities)
				{
					output.WriteLine($"  - {role.GetAttributeValue<string>("name")}");
				}

				if (command.DetailLevel == "full")
				{
					output.WriteLine();
					output.WriteLine("Privileges:", ConsoleColor.Yellow);
					// Get privileges from roles - batch role IDs to stay within Dataverse 2000-value IN clause limit
					var roleIds = roles.Entities.Select(r => r.Id).ToArray();
					var batchSize = 1000;
					var allPrivileges = new List<Entity>();

					for (int i = 0; i < roleIds.Length; i += batchSize)
					{
						var batch = roleIds.Skip(i).Take(batchSize).ToArray();
						var privQuery = new QueryExpression("privilege");
						privQuery.ColumnSet.AddColumns("name", "accesslevel", "canbebasic", "canbedeep", "canbeglobal", "canbelocal", "canbeentityreference");
						var privLink = privQuery.AddLink("roleprivileges", "privilegeid", "privilegeid");
						privLink.LinkCriteria.AddCondition("roleid", ConditionOperator.In, batch);

						var privileges = await crm.RetrieveMultipleAsync(privQuery, cancellationToken);
						allPrivileges.AddRange(privileges.Entities);
					}

					output.WriteLine($"  Total privileges: {allPrivileges.Count}");
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Security audit error: {ex.Message}", ex);
			}
		}
	}

	public class SecuritySharingReportCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<SecuritySharingReportCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SecuritySharingReportCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				if (!Guid.TryParse(command.RecordId, out var recordId))
				{
					return CommandResult.Fail($"Invalid record GUID: {command.RecordId}");
				}

				// Query PrincipalObjectAccess for shared access
				var poaQuery = new QueryExpression("principalobjectaccess");
				poaQuery.ColumnSet.AddColumns("principalid", "objectid", "accessrightsmask", "principaltypecode");
				poaQuery.Criteria.AddCondition("objectid", ConditionOperator.Equal, recordId);

				var poaResult = await crm.RetrieveMultipleAsync(poaQuery, cancellationToken);

				output.WriteLine($"Sharing Report for {command.EntityLogicalName} ({command.RecordId})", ConsoleColor.Cyan);
				output.WriteLine($"Total shared entries: {poaResult.Entities.Count}", ConsoleColor.Yellow);
				output.WriteLine();

				if (poaResult.Entities.Count > 0)
				{
					output.WriteTable(poaResult.Entities,
						() => new[] { "Principal Type", "Principal ID", "Access Rights" },
						e => new[] {
							e.GetAttributeValue<int?>("principaltypecode")?.ToString() ?? "-",
							e.GetAttributeValue<Guid?>("principalid")?.ToString() ?? "-",
							e.GetAttributeValue<int?>("accessrightsmask")?.ToString() ?? "-"
						}
					);
				}
				else
				{
					output.WriteLine("No shared access found for this record.", ConsoleColor.Green);
				}

				return CommandResult.Success();
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail($"Sharing report error: {ex.Message}", ex);
			}
		}
	}
}
