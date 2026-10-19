using System.ServiceModel;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public class SetCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ISystemUserRepository systemUserRepository
		) : ICommandExecutor<SetCommand>
	{
		private const string UserSettingsTableName = "usersettings";

		public async Task<CommandResult> ExecuteAsync(SetCommand command, CancellationToken cancellationToken)
		{
			var provided = command.GetProvidedSettings();
			if (provided.Count == 0)
			{
				return CommandResult.Fail(
					"No user setting was specified. Provide at least one option, e.g. --uilanguageid 1033. " +
					"Run 'pacx help usersettings set' to see the full list of supported options.");
			}

			output.Write("Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var providedLanguages = provided
					.Where(p => UserSettingRegistry.LanguageFieldNames.Contains(p.Key))
					.Select(p => new { FieldName = p.Key, Lcid = (int)p.Value })
					.ToList();

				if (providedLanguages.Count > 0)
				{
					output.Write("Validating language LCIDs in Dataverse...");
					var langResponse = (RetrieveAvailableLanguagesResponse)await crm.ExecuteAsync(new RetrieveAvailableLanguagesRequest());
					var available = langResponse.LocaleIds ?? [];
					var missing = providedLanguages
						.Where(p => !available.Contains(p.Lcid))
						.Select(p => $"{p.Lcid} ({p.FieldName})")
						.ToList();
					if (missing.Count > 0)
					{
						output.WriteLine("Failed", ConsoleColor.Red);
						return CommandResult.Fail(
							$"The following LCID(s) are not available in this Dataverse environment: {string.Join(", ", missing)}. " +
							"Use 'pacx org language list' to see the provisioned languages.");
					}
					output.WriteLine("Done", ConsoleColor.Green);
				}

				Guid targetUserId;
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
					output.WriteLine($"Done (user: {user.DisplayName})", ConsoleColor.Green);
				}
				else
				{
					output.Write("Retrieving current user...");
					var whoAmI = (WhoAmIResponse)await crm.ExecuteAsync(new WhoAmIRequest());
					targetUserId = whoAmI.UserId;
					output.WriteLine("Done", ConsoleColor.Green);
				}

				output.WriteLine($"Updating {provided.Count} user setting(s):");
				var userSettings = new Entity(UserSettingsTableName) { Id = targetUserId };
				foreach (var (fieldName, value) in provided)
				{
					if (UserSettingRegistry.TryGet(fieldName, out var def))
					{
						output.WriteLine($"  - {def.DisplayName} ({fieldName}) = {value}");
						if (def.EnumType is not null && value is int intCode)
							userSettings[fieldName] = new OptionSetValue(intCode);
						else
							userSettings[fieldName] = value;
					}
				}

				output.Write("Applying changes...");
				await crm.UpdateAsync(userSettings);
				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["SystemUserId"] = targetUserId;
				foreach (var (fieldName, value) in provided)
					result[fieldName] = value;
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
