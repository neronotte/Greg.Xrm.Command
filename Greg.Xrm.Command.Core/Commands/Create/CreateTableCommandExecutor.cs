using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Pluralization;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Create
{
    public class CreateTableCommandExecutor : ICommandExecutor<CreateTableCommand>
    {
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly IPluralizationFactory pluralizationFactory;
        private readonly ISettingsRepository settingsRepository;

        public CreateTableCommandExecutor(
            IOutput output,
            IOrganizationServiceRepository organizationServiceFactory,
            IPluralizationFactory pluralizationFactory,
            ISettingsRepository settingsRepository)
        {
			this.output = output;
			organizationServiceRepository = organizationServiceFactory;
            this.pluralizationFactory = pluralizationFactory;
            this.settingsRepository = settingsRepository;
        }

        public async Task ExecuteAsync(CreateTableCommand command, CancellationToken cancellationToken)
        {
            int defaultLanguageCode = 1033;


            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();

            try
            {
                var currentSolutionName = command.SolutionName;
                if (string.IsNullOrWhiteSpace(currentSolutionName))
                {
                    currentSolutionName = await this.settingsRepository.GetAsync<string>("currentSolutionName");
                    if (currentSolutionName == null)
                    {
                        output.WriteLine("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.", ConsoleColor.Red);
                        return;
                    }
                }


                this.output.WriteLine("Checking solution existence and retrieving publisher prefix");

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
                    output.WriteLine("Invalid solution name: ", ConsoleColor.Red).WriteLine(currentSolutionName, ConsoleColor.Red);
                    return;
				}

				var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
				if (managed)
				{
					output.WriteLine("The provided solution is managed. You must specify an unmanaged solution.", ConsoleColor.Red);
					return;
				}

				var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
                if (string.IsNullOrWhiteSpace(publisherPrefix))
				{
					output.WriteLine("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
					return;
				}



                this.output.Write("Setting up CreateEntityRequest...");

				var entityMetadata = new EntityMetadata();
				entityMetadata.DisplayName = SetTableDisplayName(command, defaultLanguageCode);
				entityMetadata.DisplayCollectionName = await SetTableDisplayCollectionNameAsync(command, defaultLanguageCode);
				entityMetadata.Description = SetTableDescription(command, defaultLanguageCode);
				entityMetadata.SchemaName = SetTableSchemaName(command, publisherPrefix);
				entityMetadata.OwnershipType = command.Ownership;
				entityMetadata.IsActivity = command.IsActivity;
                entityMetadata.IsAuditEnabled = SetTableIsAuditEnabled(command);


				var primaryAttribute = new StringAttributeMetadata();
				primaryAttribute.DisplayName = SetPrimaryAttributeDisplayName(command, defaultLanguageCode);
				primaryAttribute.SchemaName = SetPrimaryAttributeSchemaName(command, primaryAttribute.DisplayName, publisherPrefix);
				primaryAttribute.Description = SetPrimaryAttributeDescription(command, defaultLanguageCode);
                primaryAttribute.RequiredLevel = SetPrimaryAttributeRequiredLevel(command);
				primaryAttribute.MaxLength = SetPrimaryAttributeMaxLength(command);
				primaryAttribute.AutoNumberFormat = SetPrimaryAttributeAutoNumberFormat(command);
				primaryAttribute.FormatName = StringFormatName.Text;

				this.output.WriteLine(" Done", ConsoleColor.Green);


				this.output.Write("Creating table...");
				var request = new CreateEntityRequest
				{
					Entity = entityMetadata,
					PrimaryAttribute = primaryAttribute
				};
				var response = (CreateEntityResponse)await crm.ExecuteAsync(request);
				this.output.WriteLine(" Done", ConsoleColor.Green);


				this.output.Write("Adding table to solution...");
                var request1 = new AddSolutionComponentRequest
                {
                    AddRequiredComponents = true,
                    ComponentId = response.EntityId,
                    ComponentType = 1, // Entity
					SolutionUniqueName = currentSolutionName,
					DoNotIncludeSubcomponents = false
				};

				await crm.ExecuteAsync(request1);
				this.output.WriteLine(" Done", ConsoleColor.Green);
			}
            catch(FaultException<OrganizationServiceFault> ex)
			{
                output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);

				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}
        }

		private static BooleanManagedProperty SetTableIsAuditEnabled(CreateTableCommand command)
		{
			return new BooleanManagedProperty(command.IsAuditEnabled);
		}

		private static string SetTableSchemaName(CreateTableCommand command, string publisherPrefix)
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








        private static Label SetTableDisplayName(CreateTableCommand command, int defaultLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(command.DisplayName))
                throw new ArgumentException($"The display name is required");

            return new Label(command.DisplayName, defaultLanguageCode);
        }



        private async Task<Label> SetTableDisplayCollectionNameAsync(CreateTableCommand command, int defaultLanguageCode)
        {
            if (!string.IsNullOrWhiteSpace(command.DisplayCollectionName))
                return new Label(command.DisplayCollectionName, defaultLanguageCode);

            var pluralizer = pluralizationFactory.CreateFor(defaultLanguageCode);
            var pluralName = await pluralizer.GetPluralForAsync(command.DisplayName ?? string.Empty);

            return new Label(pluralName, defaultLanguageCode);
        }



        private static Label? SetTableDescription(CreateTableCommand command, int defaultLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(command.Description))
                return null;

            return new Label(command.Description, defaultLanguageCode);
        }


        private static string? SetPrimaryAttributeAutoNumberFormat(CreateTableCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.PrimaryAttributeAutoNumber)) return null;
            return command.PrimaryAttributeAutoNumber ?? string.Empty;
        }






        private static int SetPrimaryAttributeMaxLength(CreateTableCommand command)
        {
            if (command.PrimaryAttributeMaxLength != null)
            {
                if (command.PrimaryAttributeMaxLength < 1)
                    throw new ArgumentException($"The primary attribute max length must be greater than 0. Current value is <{command.PrimaryAttributeMaxLength}>");

                return command.PrimaryAttributeMaxLength.Value;
            }

            return 100;
        }






        private static Label SetPrimaryAttributeDisplayName(CreateTableCommand command, int defaultLanguageCode)
        {
            if (command.PrimaryAttributeDisplayName != null)
            {
                return new Label(command.PrimaryAttributeDisplayName, defaultLanguageCode);
            }

            return new Label("Name", defaultLanguageCode);
        }





        private static string SetPrimaryAttributeSchemaName(CreateTableCommand command, Label displayNameLabel, string publisherPrefix)
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

        private static Label? SetPrimaryAttributeDescription(CreateTableCommand command, int defaultLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(command.PrimaryAttributeDescription))
                return null;

            return new Label(command.PrimaryAttributeDescription, defaultLanguageCode);
		}

		private static AttributeRequiredLevelManagedProperty SetPrimaryAttributeRequiredLevel(CreateTableCommand command)
		{
			if (command.PrimaryAttributeRequiredLevel == null)
				return new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired);

            return new AttributeRequiredLevelManagedProperty(command.PrimaryAttributeRequiredLevel.Value);
		}
	}
}
