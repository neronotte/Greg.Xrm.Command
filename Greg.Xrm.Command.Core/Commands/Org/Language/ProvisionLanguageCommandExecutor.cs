using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Org.Language
{
	public class ProvisionLanguageCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
		) : ICommandExecutor<ProvisionLanguageCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ProvisionLanguageCommand command, CancellationToken cancellationToken)
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
					output.WriteLine("The selected language is the base language for the organization, it is already provisioned.", ConsoleColor.Yellow);
					return CommandResult.Success();
				}


				var request = new RetrieveInstalledLanguagePacksRequest();
				var response = (RetrieveInstalledLanguagePacksResponse)await crm.ExecuteAsync(request);
				var installedLocaleIds = response.RetrieveInstalledLanguagePacks.ToList();

				if (!installedLocaleIds.Contains(command.LanguageCode))
				{
					output.WriteLine("Failed", ConsoleColor.Red);
					return CommandResult.Fail($"The language code {command.LanguageCode} is not installed in the organization, cannot be provisioned.");
				}

				output.WriteLine("Done", ConsoleColor.Green);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);

				return CommandResult.Fail($"Error during language code validation", ex);
			}


			output.Write($"Provisioning language {command.LanguageCode}...");
			try
			{
				var request = new ProvisionLanguageAsyncRequest()
				{
					Language = command.LanguageCode
				};

				var response = (ProvisionLanguageAsyncResponse)await crm.ExecuteAsync(request);
				var asyncOperationId = response.AsyncOperationId;
				output.WriteLine("Async Operation launched, polling", ConsoleColor.Green);

				var spinner = new Spinner();
				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();
					
					Console.CursorLeft = 0;
					output.Write(spinner.Spin());
					output.Write(" ");


					var asyncOperation = await crm.RetrieveAsync("asyncoperation", asyncOperationId, new ColumnSet("statecode", "statuscode"));
					var stateCode = asyncOperation.GetAttributeValue<OptionSetValue>("statecode").Value;
					var statusCode = asyncOperation.GetAttributeValue<OptionSetValue>("statuscode").Value;

					var statusCodeFormatedd = asyncOperation.GetFormattedValue("statuscode");
					output.Write(statusCodeFormatedd.PadRight(50));



					if (stateCode == 3) // Completed
					{
						if (statusCode == 30) // Succeeded
						{
							output.WriteLine();
							output.WriteLine("Language provisioned successfully", ConsoleColor.Green);
							return CommandResult.Success();
						}
						else
						{
							output.WriteLine();
							output.WriteLine("Failed", ConsoleColor.Red);
							return CommandResult.Fail($"Language provisioning failed with status code {statusCode}");
						}
					}
					await Task.Delay(1000, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail($"Error during language provisioning", ex);
			}
		}
	}
}
