using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	public class RoleAssignmentService(
		IOutput output,
		IOrganizationServiceRepository connections,
		ISystemUserRepository users,
		Organization.Repository organizations,
		BusinessUnit.Repository businessUnits,
		Team.Repository teams,
		SecurityRole.Repository roles)
	{
		private sealed record Recipient(EntityReference Reference, string Name, Guid? BusinessUnitId);
		private sealed record Assignment(Recipient Recipient, SecurityRole Role, bool Exists);

		public async Task<CommandResult> ExecuteAsync(RoleAssignmentCommand command, bool revoke, CancellationToken cancellationToken)
		{
			var changed = 0;
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				output.WriteLine("Done", ConsoleColor.Green);

				output.Write("Checking record ownership across business units...");
				var organization = await organizations.GetAsync(crm, cancellationToken);
				var crossBusinessUnit = organization.OwnershipAcrossBusinessUnitsEnabled;
				output.WriteLine(crossBusinessUnit ? "Enabled" : "Disabled", ConsoleColor.Green);
				Guid? selectedBusinessUnit = null;
				if (crossBusinessUnit)
				{
					if (string.IsNullOrWhiteSpace(command.BusinessUnit))
						return CommandResult.Fail("Option --businessunit is required when record ownership across business units is enabled.");
					selectedBusinessUnit = (await businessUnits.ResolveAsync(crm, command.BusinessUnit.Trim(), cancellationToken)).Id;
				}
				else if (!string.IsNullOrWhiteSpace(command.BusinessUnit))
				{
					output.WriteLine("Ignoring --businessunit because record ownership across business units is disabled.");
				}

				output.Write("Resolving recipients and security roles...");
				var recipients = new List<Recipient>();
				if (!string.IsNullOrWhiteSpace(command.User))
					recipients.Add(await ResolveUserAsync(crm, command.User.Trim(), cancellationToken));
				if (!string.IsNullOrWhiteSpace(command.Team))
				{
					var team = await teams.ResolveAsync(crm, command.Team.Trim(), cancellationToken);
					if (team.teamtype?.Value == 1)
						return CommandResult.Fail($"Team '{team.name}' is an access team and cannot have security roles.");
					recipients.Add(new Recipient(new EntityReference("team", team.Id), team.name, team.businessunitid?.Id));
				}

				var assignments = new List<Assignment>();
				foreach (var recipient in recipients)
				{
					var businessUnitId = selectedBusinessUnit ?? recipient.BusinessUnitId;
					if (!businessUnitId.HasValue || businessUnitId.Value == Guid.Empty)
						return CommandResult.Fail($"No business unit found for '{recipient.Name}'.");
					var role = await roles.ResolveAsync(crm, command.Role.Trim(), businessUnitId.Value, cancellationToken);
					var exists = await roles.IsAssignedAsync(crm, role.Id, recipient.Reference, cancellationToken);
					assignments.Add(new Assignment(recipient, role, exists));
				}
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				var failures = new List<(string Message, Exception Exception)>();
				var skipped = 0;
				foreach (var assignment in assignments)
				{
					var recipient = assignment.Recipient;
					var prefix = recipient.Reference.LogicalName == "systemuser" ? "User" : "Team";
					var shouldChange = assignment.Exists == revoke;
					var didChange = false;
					if (!shouldChange)
					{
						output.WriteLine($"{prefix} '{recipient.Name}': role '{assignment.Role.name}' is {(revoke ? "not assigned" : "already assigned")}. Nothing to do.");
						skipped++;
					}
					else
					{
						try
						{
							cancellationToken.ThrowIfCancellationRequested();
							output.Write($"{(revoke ? "Revoking" : "Assigning")} role '{assignment.Role.name}' {(revoke ? "from" : "to")} {prefix.ToLowerInvariant()} '{recipient.Name}'...");
							var relationship = new Microsoft.Xrm.Sdk.Relationship(prefix == "User" ? "systemuserroles_association" : "teamroles_association");
							var related = new EntityReferenceCollection { new EntityReference("role", assignment.Role.Id) };
							OrganizationRequest request = revoke
								? new DisassociateRequest { Target = recipient.Reference, Relationship = relationship, RelatedEntities = related }
								: new AssociateRequest { Target = recipient.Reference, Relationship = relationship, RelatedEntities = related };
							await crm.ExecuteAsync(request, cancellationToken);
							changed++;
							didChange = true;
							output.WriteLine("Done", ConsoleColor.Green);
						}
						catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
						catch (Exception exception)
						{
							var message = $"{prefix} '{recipient.Name}': {exception.Message}";
							failures.Add((message, exception));
							result[prefix + "Error"] = exception.Message;
							output.WriteLine($"Failed. {message}", ConsoleColor.Red);
						}
					}
					result[prefix + "Id"] = recipient.Reference.Id;
					result[prefix + "RoleId"] = assignment.Role.Id;
					result[prefix + "BusinessUnitId"] = assignment.Role.businessunitid?.Id ?? selectedBusinessUnit ?? recipient.BusinessUnitId!.Value;
					result[prefix + "Changed"] = didChange;
				}
				result["ChangedCount"] = changed;
				result["SkippedCount"] = skipped;
				result["FailedCount"] = failures.Count;
				if (failures.Count > 0)
				{
					var failure = CommandResult.Fail(string.Join(" ", failures.Select(item => item.Message)) + $" Completed changes: {changed}. Failed changes: {failures.Count}.", failures[0].Exception);
					foreach (var entry in result) failure[entry.Key] = entry.Value;
					return failure;
				}
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception exception)
			{
				return CommandResult.Fail($"{exception.Message} Completed changes: {changed}.", exception);
			}
		}

		private async Task<Recipient> ResolveUserAsync(IOrganizationServiceAsync2 crm, string identifier, CancellationToken cancellationToken)
		{
			IReadOnlyList<SystemUser> matches;
			if (Guid.TryParse(identifier, out var userId))
			{
				var user = await users.GetByIdAsync(crm, userId, cancellationToken);
				matches = user == null ? [] : [user];
			}
			else
			{
				matches = await users.GetByDomainNameOrEmailAsync(crm, identifier, 2, cancellationToken);
			}
			if (matches.Count == 0)
				throw new InvalidOperationException($"User '{identifier}' was not found.");
			if (matches.Count > 1)
				throw new InvalidOperationException($"Multiple users match '{identifier}'. Specify the GUID with --user.");
			return new Recipient(new EntityReference("systemuser", matches[0].Id), matches[0].DisplayName, matches[0].BusinessUnit?.Id);
		}
	}
}