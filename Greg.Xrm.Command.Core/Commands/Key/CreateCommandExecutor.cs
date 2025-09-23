using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Key
{
	public class CreateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository
	)
	: ICommandExecutor<CreateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{

				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();

				var currentSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (currentSolutionName == null)
					{
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
					}
				}



				output.WriteLine("Checking solution existence and retrieving publisher prefix");

				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
				var link = query.AddLink("publisher", "publisherid", "publisherid");
				link.Columns.AddColumns("customizationprefix");
				link.EntityAlias = "publisher";
				query.NoLock = true;
				query.TopCount = 1;

				var solutionList = (await crm.RetrieveMultipleAsync(query)).Entities;
				if (solutionList.Count == 0)
				{
					return CommandResult.Fail("Invalid solution name: " + currentSolutionName);
				}

				var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
				if (managed)
				{
					return CommandResult.Fail("The provided solution is managed. You must specify an unmanaged solution.");
				}

				var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
				if (string.IsNullOrWhiteSpace(publisherPrefix))
				{
					return CommandResult.Fail("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.");
				}








				var attributes = command.Columns.Split([','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (attributes.Length == 0)
				{
					output.WriteLine("No columns were specified. Please specify at least one column.", ConsoleColor.Red);
					return CommandResult.Fail("No columns were specified.");
				}




				output.Write($"Evaluating key display and schema name...");
				var keyDisplayName = GetKeyDisplayName(command);
				var keySchemaName = GetKeySchemaName(command, publisherPrefix, keyDisplayName);
				output.WriteLine($"Done", ConsoleColor.Green);



				output.Write($"Creating key '{keySchemaName}' ('{keyDisplayName}') on table '{command.Table}'...");
				var request = new CreateEntityKeyRequest
				{
					SolutionUniqueName = currentSolutionName,
					EntityName = command.Table,
					EntityKey = new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata
					{
						DisplayName = new Label(keyDisplayName, defaultLanguageCode),
						SchemaName = keySchemaName,
						KeyAttributes = attributes
					}
				};

				var response = (CreateEntityKeyResponse)await crm.ExecuteAsync(request);

				var result = CommandResult.Success();
				result["EntityKeyId"] = response.EntityKeyId;

				output.WriteLine($"Done. Id: {response.EntityKeyId}", ConsoleColor.Green);

				return result;
			}
			catch(CommandException ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);	
   				return CommandResult.Fail(ex.Message);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static string GetKeyDisplayName(CreateCommand command)
		{
			if (!string.IsNullOrWhiteSpace(command.DisplayName))
			{
				return command.DisplayName;
			}

			return $"{command.Table.ToLowerInvariant()}_key";
		}

		private static string GetKeySchemaName(CreateCommand command, string publisherPrefix, string displayName)
		{
			if (!string.IsNullOrWhiteSpace(command.SchemaName))
			{
				if (!command.SchemaName.StartsWith(publisherPrefix, StringComparison.InvariantCultureIgnoreCase))
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, "The schema name of the key must start with the publisher prefix!");
				}

				return command.SchemaName;
			}

			return $"{publisherPrefix}_{displayName.OnlyLettersNumbersOrUnderscore()}";
		}
	}
}
