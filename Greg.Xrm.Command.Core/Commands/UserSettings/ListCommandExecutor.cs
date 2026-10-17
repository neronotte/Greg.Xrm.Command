using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISystemUserRepository systemUserRepository
		) : ICommandExecutor<ListCommand>
	{
		private const string UserSettingsTableName = "usersettings";

		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				Guid targetUserId;
				string targetUserName;

				if (!string.IsNullOrWhiteSpace(command.UserDomainName))
				{
					output.Write($"Looking up user '{command.UserDomainName}'...");
					var user = await systemUserRepository.GetByDomainNameAsync(crm, command.UserDomainName, cancellationToken);
					if (user == null)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail($"No user found with domain name '{command.UserDomainName}'.");
					}

					targetUserId = user.Id;
					targetUserName = string.IsNullOrEmpty(user.FullName) ? command.UserDomainName : user.FullName;
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

			int? intVal = raw switch
			{
				OptionSetValue osv => osv.Value,
				int i => i,
				_ => null
			};

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
