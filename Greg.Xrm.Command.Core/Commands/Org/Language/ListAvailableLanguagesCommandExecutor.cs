using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Org.Language
{
	public class ListAvailableLanguagesCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<ListAvailableLanguagesCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListAvailableLanguagesCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			List<Entity> languageList;
			int orgLcid;
			output.Write("Retrieving available languages...");
			try
			{
				var query = new QueryExpression("languagelocale");
				query.ColumnSet.AddColumns("languagelocaleid", "name", "code", "localeid");
				query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); // Active languages only
				query.AddOrder("localeid", OrderType.Ascending);

				languageList = (await crm.RetrieveMultipleAsync(query)).Entities.ToList();

				var query2 = new QueryExpression("organization");
				query2.ColumnSet.AddColumns("languagecode");
				query2.TopCount = 1;

				var org = (await crm.RetrieveMultipleAsync(query2)).Entities[0];
				orgLcid = org.GetAttributeValue<int>("languagecode");

				output.WriteLine($"Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine($"Failed", ConsoleColor.Red);

				return CommandResult.Fail("Failed to retrieve available languages", ex);
			}


			List<int> installedLocaleIds;
			List<int> provisionedLocaleIds;
			output.Write("Retrieving installed languages...");
			try
			{
				var request = new RetrieveInstalledLanguagePacksRequest();

				var response = (RetrieveInstalledLanguagePacksResponse)await crm.ExecuteAsync(request);

				installedLocaleIds = response.RetrieveInstalledLanguagePacks.ToList();
				installedLocaleIds.Add(orgLcid); // The org language is always installed, but not always returned by the request


				var request2 = new RetrieveProvisionedLanguagesRequest();
				var response2 = (RetrieveProvisionedLanguagesResponse)await crm.ExecuteAsync(request2);

				provisionedLocaleIds = response2.RetrieveProvisionedLanguages.ToList();
				provisionedLocaleIds.Add(orgLcid); // The org language is always installed, but not always returned by the request


				output.WriteLine($"Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine($"Failed", ConsoleColor.Red);
				return CommandResult.Fail("Failed to retrieve installed languages", ex);
			}

			output.WriteLine();

			output.WriteTable(languageList.Where(x => installedLocaleIds.Contains(x.GetAttributeValue<int>("localeid"))).ToList(),
				() => ["Locale ID", "Name", "Code", "Installed"],
				row => [
					row.GetAttributeValue<int>("localeid").ToString(),
					row.GetAttributeValue<string>("name"),
					row.GetAttributeValue<string>("code"),
					provisionedLocaleIds.Contains(row.GetAttributeValue<int>("localeid")) ? "Yes" : string.Empty
				],
				(rowIndex, row) =>
				{
					var localeId = row.GetAttributeValue<int>("localeid");
					if (provisionedLocaleIds.Contains(localeId))
						return ConsoleColor.White;
					return ConsoleColor.DarkGray;
				});

			return CommandResult.Success();
		}
	}
}
