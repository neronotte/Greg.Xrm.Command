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
			// ── 1. Validate key/value count match ────────────────────────────────────
			if (command.Keys.Count != command.Values.Count)
				return CommandResult.Fail(
					$"The number of --key arguments ({command.Keys.Count}) must match the number of --value arguments ({command.Values.Count}).");

			if (command.Keys.Count == 0)
				return CommandResult.Fail("At least one --key / --value pair must be provided.");

			// ── 2. Validate all keys and values locally (before connecting) ──────────
			var pairs = new List<(UserSettingDefinition FieldDef, object ParsedValue)>(command.Keys.Count);
			for (int i = 0; i < command.Keys.Count; i++)
			{
				var key = command.Keys[i];
				var rawValue = command.Values[i];

				if (!UserSettingRegistry.Fields.TryGetValue(key, out var fieldDef))
				{
					var supportedKeys = string.Join(", ", UserSettingRegistry.Fields.Keys.OrderBy(k => k));
					return CommandResult.Fail(
						$"'{key}' is not a supported usersettings field. " +
						$"Supported fields: {supportedKeys}. " +
						$"See https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/usersettings for details.");
				}

				var (isValid, validationError, parsedValue) = fieldDef.Validate(rawValue);
				if (!isValid)
					return CommandResult.Fail(validationError!);

				pairs.Add((fieldDef, parsedValue!));
			}

			// ── 3. Connect ───────────────────────────────────────────────────────────
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// ── 4. Extra validation for language fields (fetch available languages once) ──
				var languagePairs = pairs.Where(p => p.FieldDef.FieldType == UserSettingFieldType.Language).ToList();
				if (languagePairs.Count > 0)
				{
					output.Write("Validating language LCID(s) in Dataverse...");
					var langResponse = (RetrieveAvailableLanguagesResponse)await crm.ExecuteAsync(new RetrieveAvailableLanguagesRequest());
					var available = langResponse.LocaleIds ?? [];

					foreach (var (fieldDef, parsedValue) in languagePairs)
					{
						var lcid = (int)parsedValue;
						if (!available.Contains(lcid))
						{
							output.WriteLine("Failed", ConsoleColor.Red);
							return CommandResult.Fail(
								$"LCID {lcid} (field '{fieldDef.FieldName}') is not available in this Dataverse environment. " +
								$"Use 'pacx org language list' to see the provisioned languages.");
						}
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

				// ── 6. Apply all updates in a single call ────────────────────────────
				var fieldNames = string.Join(", ", pairs.Select(p => $"'{p.FieldDef.FieldName}'"));
				output.Write($"Setting {pairs.Count} field(s) ({fieldNames})...");
				var userSettings = new Entity(UserSettingsTableName) { Id = targetUserId };
				foreach (var (fieldDef, parsedValue) in pairs)
					userSettings[fieldDef.FieldName] = parsedValue;

				await crm.UpdateAsync(userSettings);
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["SystemUserId"] = targetUserId;
				result["Fields"] = string.Join(", ", pairs.Select(p => p.FieldDef.FieldName));
				for (int i = 0; i < command.Keys.Count; i++)
					result[command.Keys[i]] = command.Values[i];
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
