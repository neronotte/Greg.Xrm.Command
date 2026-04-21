using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<ListCommand>
	{
		private const string UserSettingsTableName = "usersettings";
		private const string SystemUserTableName = "systemuser";

		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			// ── 1. Connect ───────────────────────────────────────────────────────────
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				// ── 2. Resolve target user ───────────────────────────────────────────
				Guid targetUserId;
				string targetUserName;

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
					targetUserName = userResult.Entities[0].GetAttributeValue<string>("fullname") ?? command.UserDomainName;
					output.WriteLine($"Done (user: {targetUserName})", ConsoleColor.Green);
				}
				else
				{
					output.Write("Retrieving current user...");
					var whoAmI = (WhoAmIResponse)await crm.ExecuteAsync(new WhoAmIRequest());
					targetUserId = whoAmI.UserId;
					targetUserName = targetUserId.ToString();
					output.WriteLine("Done", ConsoleColor.Green);
				}

				// ── 3. Retrieve usersettings ─────────────────────────────────────────
				output.Write("Retrieving user settings...");
				var fieldNames = UserSettingRegistry.Fields.Select(f => f.FieldName).ToArray();
				var query = new QueryExpression(UserSettingsTableName);
				query.ColumnSet.AddColumns(fieldNames);
				query.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, targetUserId);
				query.TopCount = 1;

				var settingsResult = await crm.RetrieveMultipleAsync(query);
				if (settingsResult.Entities.Count == 0)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"No usersettings record found for user '{targetUserName}'. The user might not have a personalisation record yet.");
				}

				var settings = settingsResult.Entities[0];
				output.WriteLine("Done", ConsoleColor.Green);

				// ── 4. Display table ─────────────────────────────────────────────────
				output.WriteLine();
				var rows = UserSettingRegistry.Fields
					.OrderBy(d => d.FieldName)
					.ToList();

				output.WriteTable(
					rows,
					() => ["Key", "Display Name", "Value"],
					row =>
					[
						row.FieldName,
						row.DisplayName,
						FormatValue(row, settings)
					]);

				var result = CommandResult.Success();
				result["SystemUserId"] = targetUserId;
				foreach (var def in rows)
					result[def.FieldName] = FormatValue(def, settings);
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

		private static string FormatValue(UserSettingField field, Entity settings)
		{
			if (!settings.Contains(field.FieldName))
				return string.Empty;

			var raw = settings[field.FieldName];
			if (raw == null)
				return string.Empty;

			// Dataverse returns optionset-typed columns as OptionSetValue; normalise to int.
			int? intVal = raw switch
			{
				OptionSetValue osv => osv.Value,
				int i => i,
				_ => null
			};

			// Picklist fields carry an enum type: render "<code> (<Name>)" when the code is known.
			if (field.EnumType is not null && intVal.HasValue)
			{
				return Enum.IsDefined(field.EnumType, intVal.Value)
					? $"{intVal.Value} ({Enum.GetName(field.EnumType, intVal.Value)})"
					: intVal.Value.ToString();
			}

			return raw switch
			{
				bool b => b ? "true" : "false",
				_ => intVal?.ToString() ?? raw.ToString() ?? string.Empty
			};
		}
	}
}
