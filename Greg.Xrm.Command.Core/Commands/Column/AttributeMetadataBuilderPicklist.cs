using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
    public class AttributeMetadataBuilderPicklist : AttributeMetadataBuilderBase
    {
        public override AttributeMetadata CreateFrom(CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new PicklistAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            var optionSet = new OptionSetMetadata();
            optionSet.OptionSetType = OptionSetType.Picklist;
            optionSet.IsCustomOptionSet = true;
            optionSet.IsGlobal = false;
            optionSet.IsCustomizable = new BooleanManagedProperty(true);
            optionSet.Name = GetOptionSetName(command.EntityName, attribute.SchemaName); // se non è global. Se è global, il nome è quello del global option set
            optionSet.DisplayName = attribute.DisplayName;
            optionSet.Description = attribute.Description;


            var optionString = command.Options;
            if (string.IsNullOrWhiteSpace(optionString))
                throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The options are required for columns of type Picklist");

            var optionArray = optionString.Split(',', '|', StringSplitOptions.RemoveEmptyEntries);
            if (optionArray.Length == 0)
                throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The options are required for columns of type Picklist");

            for (int i = 0; i < optionArray.Length; i++)
            {
                var optionName = optionArray[i];
                var optionValue = customizationOptionValuePrefix * 10000 + i;

                optionSet.Options.Add(new OptionMetadata(new Label(optionName, languageCode), optionValue));
            }


            attribute.DefaultFormValue = null;
            attribute.OptionSet = optionSet;

            return attribute;
        }

        private static string GetOptionSetName(string? entityName, string schemaName)
        {
            return $"{entityName}_{schemaName}";
        }

        protected override string GetSchemaName(string? displayName, string? schemaName, string publisherPrefix)
        {
            var newSchemaName = base.GetSchemaName(displayName, schemaName, publisherPrefix);

            if (!newSchemaName.EndsWith("code"))
            {
                newSchemaName += "code";
            }

            return newSchemaName;
        }
    }
}
