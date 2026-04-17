using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.Globalization;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public class ChangeLanguageCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<ChangeLanguageCommand>
	{
		private const string UserSettingsTableName = "usersettings";
		private const string UiLanguageIdFieldName = "uilanguageid";
		private const string HelpLanguageIdFieldName = "helplanguageid";
		private const string LocaleIdFieldName = "localeid";

		public async Task<CommandResult> ExecuteAsync(ChangeLanguageCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Validating LCID {command.Lcid}...");
			try
			{
				_ = CultureInfo.GetCultureInfo(command.Lcid);
				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (CultureNotFoundException)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"The LCID {command.Lcid} is not valid.");
			}
			catch (ArgumentOutOfRangeException)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"The LCID {command.Lcid} is not valid.");
			}


			output.Write($"Connecting to the current Dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				output.Write($"Validating language {command.Lcid} in Dataverse...");
				var languageRequest = new RetrieveAvailableLanguagesRequest();
				var languageResponse = (RetrieveAvailableLanguagesResponse)await crm.ExecuteAsync(languageRequest);
				var availableLanguages = languageResponse.LocaleIds ?? [];
				if (!availableLanguages.Contains(command.Lcid))
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"The language code {command.Lcid} is not available in the current Dataverse environment.");
				}
				output.WriteLine("Done", ConsoleColor.Green);


				output.Write("Retrieving current user...");
				var whoAmIResponse = (WhoAmIResponse)await crm.ExecuteAsync(new WhoAmIRequest());
				output.WriteLine("Done", ConsoleColor.Green);


				output.Write("Updating user settings...");
				var userSettings = new Entity(UserSettingsTableName)
				{
					Id = whoAmIResponse.UserId
				};

				if (command.Field is null || command.Field == LanguageField.UiLanguageId)
				{
					userSettings[UiLanguageIdFieldName] = command.Lcid;
				}
				if (command.Field is null || command.Field == LanguageField.HelpLanguageId)
				{
					userSettings[HelpLanguageIdFieldName] = command.Lcid;
				}
				if (command.Field is null || command.Field == LanguageField.LocaleId)
				{
					userSettings[LocaleIdFieldName] = command.Lcid;
				}

				await crm.UpdateAsync(userSettings);
				output.WriteLine("Done", ConsoleColor.Green);


				var result = CommandResult.Success();
				result["SystemUserId"] = whoAmIResponse.UserId;
				result["Lcid"] = command.Lcid;
				result["Field"] = command.Field?.ToString() ?? "All";
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
