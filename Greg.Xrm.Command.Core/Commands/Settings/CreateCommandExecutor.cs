using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using System.Globalization;

namespace Greg.Xrm.Command.Commands.Settings
{
    public class CreateCommandExecutor : ICommandExecutor<CreateCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public CreateCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
		}



		public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(command.DisplayName))
			{
				return CommandResult.Fail( "The display name is required." );
			}


			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			var (publisherPrefix, currentSolutionName, _) = await CheckSolutionAndReturnPublisherPrefixAsync(crm, command.SolutionName);
			if (publisherPrefix == null) return CommandResult.Fail("No publisher prefix found");
			if (currentSolutionName == null) return CommandResult.Fail("No solution name found");

			var settingName = command.Name;
			if (string.IsNullOrWhiteSpace(settingName))
			{
				var namePart = command.DisplayName.OnlyLettersNumbersOrUnderscore();
				if (string.IsNullOrWhiteSpace(namePart))
					return CommandResult.Fail($"Is not possible to infer the setting name from the display name, please explicit a setting name.");

				settingName = $"{publisherPrefix}_{namePart}";
			}
			else
			{
				if (!settingName.StartsWith(publisherPrefix + "_"))
					return CommandResult.Fail($"The setting schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{settingName.Split("_").FirstOrDefault()}>");
			}


			if (!TryParseDefaultValue(command.DataType, command.DefaultValue, out var defaultValue))
				return CommandResult.Fail($"The default value <{command.DefaultValue}> is not a valid value for the data type <{command.DataType}>");


			try
			{
				this.output.Write($"Creating setting {settingName}...");

				var setting = new SettingDefinition
				{
					displayname = command.DisplayName,
					uniquename = settingName,
					description = command.Description,
					datatype = new OptionSetValue((int)command.DataType),
					defaultvalue = defaultValue,
					isoverridable = command.OverridableLevel != OverridableLevel.None,
					overridablelevel = ParseLevel(command.OverridableLevel),
					releaselevel = new OptionSetValue((int)command.ReleaseLevel),
					informationurl = command.InformationUrl
				};

				await setting.SaveOrUpdateAsync(crm);

				this.output.WriteLine("Done", ConsoleColor.Green);

				this.output.Write("Adding setting to the solution...");

				var request = new AddSolutionComponentRequest
				{
					SolutionUniqueName = currentSolutionName,
					ComponentType = (int)ComponentType.SettingDefinition, // SettingDefinition
					ComponentId = setting.Id
				};

				var response = (AddSolutionComponentResponse)(await crm.ExecuteAsync(request));

				this.output.WriteLine("Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Setting Id"] = setting.Id;
				result["Setting Unique Name"] = settingName;
				result["Solution Component Id"] = response.id;
				return result;
			}
			catch(FaultException<OrganizationServiceFault> ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				this.output.WriteLine(ex.Message, ConsoleColor.Red);

				return CommandResult.Fail("Error creating setting: " + ex.Message, ex);
			}
		}

		public static OptionSetValue ParseLevel(OverridableLevel overridableLevel)
		{
			if (overridableLevel == OverridableLevel.EnvApp) return new OptionSetValue((int)SettingDefinitionOverridableLevel.AppAndOrganization);
			if (overridableLevel == OverridableLevel.Env) return new OptionSetValue((int)SettingDefinitionOverridableLevel.Organization);
			if (overridableLevel == OverridableLevel.App) return new OptionSetValue((int)SettingDefinitionOverridableLevel.App);
			return new OptionSetValue((int)SettingDefinitionOverridableLevel.AppAndOrganization); // None maps here
		}


		private static bool TryParseDefaultValue(SettingDefinitionDataType dataType, string? defaultValue, out string? result)
		{
			result = null;
			if (string.IsNullOrWhiteSpace(defaultValue))
				return true;

			if (dataType.Equals(SettingDefinitionDataType.Number))
			{
				if (decimal.TryParse(defaultValue, out var number))
				{
					result = number.ToString(CultureInfo.InvariantCulture);
					return true;
				}
				return false;
			}


			if (dataType.Equals(SettingDefinitionDataType.Boolean))
			{
				if (bool.TryParse(defaultValue, out var boolean))
				{
					result = boolean.ToString().ToLowerInvariant();
					return true;
				}

				if (int.TryParse(defaultValue, out var intBoolean))
				{
					result = (intBoolean != 0).ToString().ToLowerInvariant();
					return true;
				}
				return false;
			}

			result = defaultValue;
			return true;
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
