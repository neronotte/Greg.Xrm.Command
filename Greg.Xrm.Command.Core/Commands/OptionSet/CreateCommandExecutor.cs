using Autofac.Core;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.OptionSet;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	public class CreateCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			IOptionSetParser optionSetParser) 
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

				var (publisherPrefix, currentSolutionName, customizationOptionValuePrefix) = await CheckSolutionAndReturnPublisherPrefixAsync(crm, command.SolutionName);
				if (publisherPrefix == null) return CommandResult.Fail("No publisher prefix found");
				if (currentSolutionName == null) return CommandResult.Fail("No solution name found");
				if (customizationOptionValuePrefix == null) return CommandResult.Fail("No customization option value prefix found");



				var options = optionSetParser.Parse(
					command.Options, 
					command.Colors, 
					customizationOptionValuePrefix.Value, 
					defaultLanguageCode);



				output.Write("Creating global option set...");


				var optionSetMetadata = new OptionSetMetadata
				{
					Name = GetSchemaName(command.DisplayName, command.SchemaName, publisherPrefix),
					DisplayName = GetDisplayName(command.DisplayName, defaultLanguageCode),
					IsGlobal = true,
					OptionSetType = OptionSetType.Picklist
				};
				optionSetMetadata.Options.AddRange(options);

				var request = new CreateOptionSetRequest
				{
					OptionSet = optionSetMetadata,
					SolutionUniqueName = currentSolutionName
				};
				var response = (CreateOptionSetResponse)await crm.ExecuteAsync(request);

				output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result[nameof(response.OptionSetId)] = response.OptionSetId.ToString();
				return result;
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}




		private static Label GetDisplayName(string? displayName, int defaultLanguageCode)
		{

			if (string.IsNullOrWhiteSpace(displayName))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The display name is required");

			return new Label(displayName, defaultLanguageCode);
		}


		private static string GetSchemaName(string? displayName, string? schemaName, string publisherPrefix)
		{
			if (!string.IsNullOrWhiteSpace(schemaName))
			{
				if (!schemaName.StartsWith(publisherPrefix + "_"))
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{schemaName.Split("_").FirstOrDefault()}>");

				return schemaName;
			}

			if (!string.IsNullOrWhiteSpace(displayName))
			{
				var namePart = displayName.OnlyLettersNumbersOrUnderscore();
				if (string.IsNullOrWhiteSpace(namePart))
					throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the primary attribute schema name from the display name, please explicit a primary attribute schema name");

				return $"{publisherPrefix}_{namePart}";
			}

			throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"Is not possible to infer the schema name from the display name, please explicit a schema name");
		}





		private async Task<(string?, string?, int?)> CheckSolutionAndReturnPublisherPrefixAsync(IOrganizationServiceAsync2 crm, string? currentSolutionName)
		{
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					output.WriteLine("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.", ConsoleColor.Red);
					return (null, null, null);
				}
			}


			output.WriteLine("Checking solution existence and retrieving publisher prefix");

			var query = new QueryExpression("solution");
			query.ColumnSet.AddColumns("ismanaged");
			query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
			var link = query.AddLink("publisher", "publisherid", "publisherid");
			link.Columns.AddColumns("customizationprefix", "customizationoptionvalueprefix");
			link.EntityAlias = "publisher";
			query.NoLock = true;
			query.TopCount = 1;


			var solutionList = (await crm.RetrieveMultipleAsync(query)).Entities;
			if (solutionList.Count == 0)
			{
				output.WriteLine("Invalid solution name: ", ConsoleColor.Red).WriteLine(currentSolutionName, ConsoleColor.Red);
				return (null, null, null);
			}

			var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
			if (managed)
			{
				output.WriteLine("The provided solution is managed. You must specify an unmanaged solution.", ConsoleColor.Red);
				return (null, null, null);
			}

			var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
			if (string.IsNullOrWhiteSpace(publisherPrefix))
			{
				output.WriteLine("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
				return (null, null, null);
			}


			var customizationOptionValuePrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationoptionvalueprefix").Value as int?;
			if (customizationOptionValuePrefix == null)
			{
				output.WriteLine("Unable to retrieve the optionset prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
				return (null, null, null);
			}

			return (publisherPrefix, currentSolutionName, customizationOptionValuePrefix);
		}
	}
}
