using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
	public abstract class AttributeMetadataBuilderBase : IAttributeMetadataBuilder
    {
        public abstract Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix);





        protected void SetCommonProperties(AttributeMetadata attribute, CreateCommand command, int languageCode, string publisherPrefix)
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

    }
}
