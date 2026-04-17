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


			output.Write($"Connecting to the current dataverse environment...");
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
				var userSettings = new Entity("usersettings")
				{
					Id = whoAmIResponse.UserId
				};

				if (command.Field is null || command.Field == UserSettingsLanguageField.UiLanguageId)
				{
					userSettings["uilanguageid"] = command.Lcid;
				}
				if (command.Field is null || command.Field == UserSettingsLanguageField.HelpLanguageId)
				{
					userSettings["helplanguageid"] = command.Lcid;
				}
				if (command.Field is null || command.Field == UserSettingsLanguageField.LocaleId)
				{
					userSettings["localeid"] = command.Lcid;
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
				return CommandResult.Fail(ex.Message, ex);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
