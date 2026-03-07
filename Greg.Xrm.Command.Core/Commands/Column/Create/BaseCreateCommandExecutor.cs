using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.ServiceModel;
using System.Text;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public abstract class BaseCreateCommandExecutor<TCommand>(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		where TCommand : BaseCreateCommand
	{
		protected enum Limit { Max, Min }


		protected virtual async Task<CommandResult> ExecuteAsync(
			TCommand command, 
			Func<IOrganizationServiceAsync2, TCommand, int, string, int, Task<AttributeMetadata>> createAttributeFunc,
			CancellationToken cancellationToken)
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


				var attribute = await createAttributeFunc(crm, command, defaultLanguageCode, publisherPrefix, customizationOptionValuePrefix.Value);

				output.Write($"Creating attribute {attribute.SchemaName}...");
				var request = new CreateAttributeRequest
				{
					SolutionUniqueName = currentSolutionName,
					EntityName = command.EntityName,
					Attribute = attribute
				};

				var response = (CreateAttributeResponse)await crm.ExecuteAsync(request, cancellationToken);

				output.WriteLine("Done", ConsoleColor.Green);

				return new CreateCommandResult(response.AttributeId);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				var sb = new StringBuilder();
				sb.Append("Exception of type FaultException<OrganizationServiceFault> occurred. ");
				if (!string.IsNullOrWhiteSpace(ex.Message))
				{
					sb.AppendLine().Append("Message: ").Append(ex.Message).Append(". ");
					sb.AppendLine().Append("Details: ").Append(JsonConvert.SerializeObject(ex)).Append(". ");
				}
				if (ex.InnerException != null)
				{
					sb.AppendLine()
						.Append("Inner exception of type ")
						.Append(ex.InnerException.GetType().FullName)
						.Append(": ")
						.Append(ex.InnerException.Message)
						.Append(". ");
				}

				return CommandResult.Fail(sb.ToString(), ex);
			}
			catch (Exception ex)
			{
				var sb = new StringBuilder();
				sb.Append("Exception of type ").Append(ex.GetType().FullName).Append(" occurred. ");
				if (!string.IsNullOrWhiteSpace(ex.Message))
				{
					sb.AppendLine().Append("Message: ").Append(ex.Message).Append(". ");
				}
				if (ex.InnerException != null)
				{
					sb.AppendLine()
						.Append("Inner exception of type ")
						.Append(ex.InnerException.GetType().FullName)
						.Append(": ")
						.Append(ex.InnerException.Message)
						.Append(". ");
				}


				return CommandResult.Fail(sb.ToString(), ex);
			}
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




		protected void SetCommonProperties(AttributeMetadata attribute, BaseCreateCommand command, int languageCode, string publisherPrefix)
		{
			attribute.Description = GetDescription(command.Description, languageCode);
			attribute.DisplayName = GetDisplayName(command.DisplayName, languageCode);
			attribute.LogicalName = GetSchemaName(command.DisplayName, command.SchemaName, publisherPrefix);
			attribute.RequiredLevel = new AttributeRequiredLevelManagedProperty(command.RequiredLevel);
			attribute.SchemaName = attribute.LogicalName;
			attribute.IsAuditEnabled = new BooleanManagedProperty(command.IsAuditEnabled);


			attribute.IsValidForCreate = true;
			attribute.IsValidForUpdate = true;
			attribute.IsSecured = false;
			attribute.IsGlobalFilterEnabled = new BooleanManagedProperty(false);
			attribute.IsSortableEnabled = new BooleanManagedProperty(true);
			attribute.IsValidForAdvancedFind = new BooleanManagedProperty(true);
			attribute.IsCustomizable = new BooleanManagedProperty(true);
			attribute.IsValidForGrid = true;
			attribute.IsValidForForm = true;
			attribute.IsRequiredForForm = false;
			attribute.IsRenameable = new BooleanManagedProperty(true);
			attribute.CanModifyAdditionalSettings = new BooleanManagedProperty(true);
		}



		protected virtual Label GetDisplayName(string? displayName, int defaultLanguageCode)
		{

			if (string.IsNullOrWhiteSpace(displayName))
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The display name is required");

			return new Label(displayName, defaultLanguageCode);
		}

		protected virtual Label? GetDescription(string? description, int defaultLanguageCode)
		{
			if (string.IsNullOrWhiteSpace(description)) return null;
			return new Label(description, defaultLanguageCode);
		}


		protected virtual string GetSchemaName(string? displayName, string? schemaName, string publisherPrefix)
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

		protected static int GetIntValue(double? doubleValue, Limit limit)
		{
			var value = doubleValue == null ? (int?)null : Convert.ToInt32(Math.Floor(doubleValue.Value));

			if (limit == Limit.Min)
			{
				if (value == null) return int.MinValue;
				if (value < int.MinValue)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The minimum value must be higher than -{int.MinValue} (int.MinValue)");
				}
				return value.Value;
			}
			else
			{
				if (value == null) return int.MaxValue;
				if (value > int.MaxValue)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The maximum value must be lower than {int.MaxValue} (int.MaxValue)");
				}
				return value.Value;
			}
		}
		protected static double GetDoubleValue(double? doubleValue, Limit limit)
		{
			var value = doubleValue == null ? (double?)null : Convert.ToDouble(doubleValue.Value);

			if (limit == Limit.Min)
			{
				if (value == null) return int.MinValue;
				if (value < Int64.MinValue)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The minimum value must be higher than -{Int64.MinValue} (Int64.MinValue)");
				}
				return value.Value;
			}
			else
			{
				if (value == null) return int.MaxValue;
				if (value > Int64.MaxValue)
				{
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The maximum value must be lower than {Int64.MaxValue} (Int64.MaxValue)");
				}
				return value.Value;
			}
		}
	}
}
