using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Pluralization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Table
{
    public class CreateCommandExecutor : ICommandExecutor<CreateCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly IPluralizationFactory pluralizationFactory;

        public CreateCommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceFactory,
            IPluralizationFactory pluralizationFactory)
        {
            this.output = output;
            organizationServiceRepository = organizationServiceFactory;
            this.pluralizationFactory = pluralizationFactory;
        }

        public async Task<CommandResult> ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);


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



                output.Write("Setting up CreateEntityRequest...");

				var entityMetadata = new EntityMetadata
				{
					DisplayName = SetTableDisplayName(command, defaultLanguageCode),
					DisplayCollectionName = await SetTableDisplayCollectionNameAsync(command, defaultLanguageCode),
					Description = SetTableDescription(command, defaultLanguageCode),
					SchemaName = SetTableSchemaName(command, publisherPrefix),
					OwnershipType = command.Ownership,
					IsActivity = command.IsActivity,
					IsAuditEnabled = SetTableIsAuditEnabled(command)
				};


				var primaryAttribute = new StringAttributeMetadata
				{
					DisplayName = SetPrimaryAttributeDisplayName(command, defaultLanguageCode),
					Description = SetPrimaryAttributeDescription(command, defaultLanguageCode),
					RequiredLevel = SetPrimaryAttributeRequiredLevel(command),
					MaxLength = SetPrimaryAttributeMaxLength(command),
					AutoNumberFormat = SetPrimaryAttributeAutoNumberFormat(command),
					FormatName = StringFormatName.Text
				};
				primaryAttribute.SchemaName = SetPrimaryAttributeSchemaName(command, primaryAttribute.DisplayName, publisherPrefix);

				output.WriteLine(" Done", ConsoleColor.Green);


                output.Write("Creating table...");
                var request = new CreateEntityRequest
                {
                    Entity = entityMetadata,
                    PrimaryAttribute = primaryAttribute
                };
                var response = (CreateEntityResponse)await crm.ExecuteAsync(request);
                output.WriteLine(" Done", ConsoleColor.Green);


                output.Write("Adding table to solution...");
                var request1 = new AddSolutionComponentRequest
                {
                    AddRequiredComponents = true,
                    ComponentId = response.EntityId,
                    ComponentType = 1, // Entity
                    SolutionUniqueName = currentSolutionName,
                    DoNotIncludeSubcomponents = false
                };

                await crm.ExecuteAsync(request1);
				output.WriteLine(" Done", ConsoleColor.Green);

				var result = CommandResult.Success();
				result["Table ID"] = response.EntityId;
				result["Primary Column ID"] = response.AttributeId;
                return result;
			}
            catch (FaultException<OrganizationServiceFault> ex)
            {
                return CommandResult.Fail(ex.Message, ex);
            }
        }

        private static BooleanManagedProperty SetTableIsAuditEnabled(CreateCommand command)
        {
            return new BooleanManagedProperty(command.IsAuditEnabled);
        }

        private static string SetTableSchemaName(CreateCommand command, string publisherPrefix)
        {
            if (!string.IsNullOrWhiteSpace(command.SchemaName))
            {
                if (!command.SchemaName.StartsWith(publisherPrefix + "_"))
                    throw new ArgumentException($"The table schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{publisherPrefix.Split("_").FirstOrDefault()}>");

                return command.SchemaName ?? string.Empty;
            }



            var displayName = command.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException($"Is not possible to infer the table schema name from the display name, please explicit a table schema name");

            var namePart = displayName.OnlyLettersNumbersOrUnderscore();
            if (string.IsNullOrWhiteSpace(namePart))
                throw new ArgumentException($"Is not possible to infer the table schema name from the display name, please explicit a table schema name");

            return $"{publisherPrefix}_{namePart}";

        }








        private static Label SetTableDisplayName(CreateCommand command, int defaultLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(command.DisplayName))
                throw new ArgumentException($"The display name is required");

            return new Label(command.DisplayName, defaultLanguageCode);
        }



        private async Task<Label> SetTableDisplayCollectionNameAsync(CreateCommand command, int defaultLanguageCode)
        {
            if (!string.IsNullOrWhiteSpace(command.DisplayCollectionName))
                return new Label(command.DisplayCollectionName, defaultLanguageCode);

            var pluralizer = pluralizationFactory.CreateFor(defaultLanguageCode);
            var pluralName = await pluralizer.GetPluralForAsync(command.DisplayName ?? string.Empty);

            return new Label(pluralName, defaultLanguageCode);
        }



        private static Label? SetTableDescription(CreateCommand command, int defaultLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(command.Description))
                return null;

            return new Label(command.Description, defaultLanguageCode);
        }


        private static string? SetPrimaryAttributeAutoNumberFormat(CreateCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.PrimaryAttributeAutoNumber)) return null;
            return command.PrimaryAttributeAutoNumber ?? string.Empty;
        }






        private static int SetPrimaryAttributeMaxLength(CreateCommand command)
        {
            if (command.PrimaryAttributeMaxLength != null)
            {
                if (command.PrimaryAttributeMaxLength < 1)
                    throw new ArgumentException($"The primary attribute max length must be greater than 0. Current value is <{command.PrimaryAttributeMaxLength}>");

                return command.PrimaryAttributeMaxLength.Value;
            }

            return 100;
        }






        private static Label SetPrimaryAttributeDisplayName(CreateCommand command, int defaultLanguageCode)
        {
            if (command.PrimaryAttributeDisplayName != null)
            {
                return new Label(command.PrimaryAttributeDisplayName, defaultLanguageCode);
            }

            if (command.PrimaryAttributeAutoNumber != null)
            {
                return new Label("Code", defaultLanguageCode);
            }

            return new Label("Name", defaultLanguageCode);
        }





        private static string SetPrimaryAttributeSchemaName(CreateCommand command, Label displayNameLabel, string publisherPrefix)
        {
            if (!string.IsNullOrWhiteSpace(command.PrimaryAttributeSchemaName))
            {
                if (!command.PrimaryAttributeSchemaName.StartsWith(publisherPrefix + "_"))
                    throw new ArgumentException($"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{publisherPrefix.Split("_").FirstOrDefault()}>");

                return command.PrimaryAttributeSchemaName ?? string.Empty;
            }

            var displayName = displayNameLabel.LocalizedLabels[0].Label;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var namePart = displayName.OnlyLettersNumbersOrUnderscore();
                if (string.IsNullOrWhiteSpace(namePart))
                    throw new ArgumentException($"Is not possible to infer the primary attribute schema name from the display name, please explicit a primary attribute schema name");

                return $"{publisherPrefix}_{namePart}";
            }

            return $"{publisherPrefix}_name";
        }

        private static Label? SetPrimaryAttributeDescription(CreateCommand command, int defaultLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(command.PrimaryAttributeDescription))
                return null;

            return new Label(command.PrimaryAttributeDescription, defaultLanguageCode);
        }

        private static AttributeRequiredLevelManagedProperty SetPrimaryAttributeRequiredLevel(CreateCommand command)
        {
            if (command.PrimaryAttributeRequiredLevel != null)
                return new AttributeRequiredLevelManagedProperty(command.PrimaryAttributeRequiredLevel.Value);

            if (command.PrimaryAttributeAutoNumber != null)
                return new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None);

            return new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired);

        }
    }
}
