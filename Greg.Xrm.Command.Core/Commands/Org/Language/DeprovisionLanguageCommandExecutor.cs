using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Org.Language
{
	public class DeprovisionLanguageCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<DeprovisionLanguageCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DeprovisionLanguageCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			output.Write("Validating the language code...");
			try
			{
				var query2 = new QueryExpression("organization");
				query2.ColumnSet.AddColumns("languagecode");
				query2.TopCount = 1;

				var org = (await crm.RetrieveMultipleAsync(query2)).Entities[0];
				var orgLcid = org.GetAttributeValue<int>("languagecode");

				if (command.LanguageCode == orgLcid)
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					output.WriteLine("The selected language is the base language for the organization, it cannot be deprovisioned.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}


				var request = new RetrieveInstalledLanguagePacksRequest();
				var response = (RetrieveInstalledLanguagePacksResponse)await crm.ExecuteAsync(request);
				var installedLocaleIds = response.RetrieveInstalledLanguagePacks.ToList();

				if (!installedLocaleIds.Contains(command.LanguageCode))
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"The language code {command.LanguageCode} is not installed in the organization, cannot be deprovisioned.");
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);

				return CommandResult.Fail($"Error during language code validation", ex);
			}


			output.Write($"Deprovisioning language {command.LanguageCode}...");
			try
			{
				var request = new DeprovisionLanguageRequest()
				{
					Language = command.LanguageCode
				};

				await crm.ExecuteAsync(request);


				output.WriteLine("Done", ConsoleColor.Green);
				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Error during language provisioning", ex);
			}
		}
	}
}
