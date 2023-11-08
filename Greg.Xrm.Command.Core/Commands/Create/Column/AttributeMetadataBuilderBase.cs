using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Create.Column
{
	public abstract class AttributeMetadataBuilderBase : IAttributeMetadataBuilder
	{
		public abstract AttributeMetadata CreateFrom(CreateColumnCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix);





		protected void SetCommonProperties(AttributeMetadata attribute, CreateColumnCommand command, int languageCode, string publisherPrefix)
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
			attribute.IsGlobalFilterEnabled = new Microsoft.Xrm.Sdk.BooleanManagedProperty(false);
			attribute.IsSortableEnabled = new Microsoft.Xrm.Sdk.BooleanManagedProperty(true);
			attribute.IsValidForAdvancedFind = new Microsoft.Xrm.Sdk.BooleanManagedProperty(true);
			attribute.IsCustomizable = new Microsoft.Xrm.Sdk.BooleanManagedProperty(true);
			attribute.IsValidForGrid = true;
			attribute.IsValidForForm = true;
			attribute.IsRequiredForForm = false;
			attribute.IsRenameable = new Microsoft.Xrm.Sdk.BooleanManagedProperty(true);
			attribute.CanModifyAdditionalSettings = new Microsoft.Xrm.Sdk.BooleanManagedProperty(true);

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
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The primary attribute schema name must start with the publisher prefix. Current publisher prefix is <{publisherPrefix}>, provided value is <{publisherPrefix.Split("_").FirstOrDefault()}>");

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
