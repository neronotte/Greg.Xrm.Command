using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public class SetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<SetCommand>
	{
		private const string UserSettingsTableName = "usersettings";
		private const string SystemUserTableName = "systemuser";

		public async Task<CommandResult> ExecuteAsync(SetCommand command, CancellationToken cancellationToken)
		{
			// ── 1. Validate key ──────────────────────────────────────────────────────
			if (!UserSettingRegistry.Fields.TryGetValue(command.Key, out var fieldDef))
			{
				var supportedKeys = string.Join(", ", UserSettingRegistry.Fields.Keys.OrderBy(k => k));
				return CommandResult.Fail(
					$"'{command.Key}' is not a supported usersettings field. " +
					$"Supported fields: {supportedKeys}. " +
					$"See https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/usersettings for details.");
			}

			// ── 2. Validate value locally ────────────────────────────────────────────
			var (isValid, validationError, parsedValue) = fieldDef.Validate(command.Value);
			if (!isValid)
				return CommandResult.Fail(validationError!);

			// ── 3. Connect ───────────────────────────────────────────────────────────
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// ── 4. Extra validation for language fields (Dataverse availability) ──
				if (fieldDef.FieldType == UserSettingFieldType.Language)
				{
					var lcid = (int)parsedValue!;
					output.Write($"Validating language LCID {lcid} in Dataverse...");
					var langResponse = (RetrieveAvailableLanguagesResponse)await crm.ExecuteAsync(new RetrieveAvailableLanguagesRequest());
					var available = langResponse.LocaleIds ?? [];
					if (!available.Contains(lcid))
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail(
							$"LCID {lcid} is not available in this Dataverse environment. " +
							$"Use 'pacx org language list' to see the provisioned languages.");
					}
					output.WriteLine("Done", ConsoleColor.Green);
				}

				// ── 5. Resolve target user ───────────────────────────────────────────
				Guid targetUserId;
				if (!string.IsNullOrWhiteSpace(command.UserDomainName))
				{
					output.Write($"Looking up user '{command.UserDomainName}'...");
					var userQuery = new QueryExpression(SystemUserTableName);
					userQuery.ColumnSet.AddColumns("systemuserid", "fullname", "domainname");
					userQuery.Criteria.AddCondition("domainname", ConditionOperator.Equal, command.UserDomainName);
					userQuery.TopCount = 1;

					var userResult = await crm.RetrieveMultipleAsync(userQuery);
					if (userResult.Entities.Count == 0)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail($"No active user found with domain name '{command.UserDomainName}'.");
					}

					targetUserId = userResult.Entities[0].Id;
					var fullName = userResult.Entities[0].GetAttributeValue<string>("fullname");
					output.WriteLine($"Done (user: {fullName})", ConsoleColor.Green);
				}
				else
				{
					output.Write("Retrieving current user...");
					var whoAmI = (WhoAmIResponse)await crm.ExecuteAsync(new WhoAmIRequest());
					targetUserId = whoAmI.UserId;
					output.WriteLine("Done", ConsoleColor.Green);
				}

				// ── 6. Apply update ──────────────────────────────────────────────────
				output.Write($"Setting '{fieldDef.DisplayName}' ({fieldDef.FieldName}) to '{command.Value}'...");
				var userSettings = new Entity(UserSettingsTableName) { Id = targetUserId };
				userSettings[fieldDef.FieldName] = parsedValue;

				await crm.UpdateAsync(userSettings);
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["SystemUserId"] = targetUserId;
				result["Key"] = fieldDef.FieldName;
				result["Value"] = command.Value;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Dataverse error: {ex.Message}", ex);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
