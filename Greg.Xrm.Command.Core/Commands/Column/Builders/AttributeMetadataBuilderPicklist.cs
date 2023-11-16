using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    public class AttributeMetadataBuilderPicklist : AttributeMetadataBuilderBase
    {
        public override async Task<AttributeMetadata> CreateFromAsync(
            IOrganizationServiceAsync2 crm,
            CreateCommand command,
            int languageCode,
            string publisherPrefix,
            int customizationOptionValuePrefix)
        {
            EnumAttributeMetadata attribute = command.Multiselect ? new MultiSelectPicklistAttributeMetadata() : new PicklistAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            var optionSet = new OptionSetMetadata();
            optionSet.DisplayName = attribute.DisplayName;
            optionSet.Description = attribute.Description;

            if (command.GlobalOptionSetName != null)
            {
                try
                {
                    var request = new RetrieveOptionSetRequest { Name = command.GlobalOptionSetName };

                    var response = (RetrieveOptionSetResponse)await crm.ExecuteAsync(request);

                    if (response.OptionSetMetadata.OptionSetType == OptionSetType.Boolean)
                        throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The global option set '{command.GlobalOptionSetName}' is of type Boolean, that is not yet supported.");

                    optionSet.OptionSetType = response.OptionSetMetadata.OptionSetType;
                    optionSet.IsGlobal = response.OptionSetMetadata.IsGlobal;
                    optionSet.IsCustomizable = response.OptionSetMetadata.IsCustomizable;
                    optionSet.IsCustomOptionSet = response.OptionSetMetadata.IsCustomOptionSet;
                    optionSet.Name = response.OptionSetMetadata.Name;
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The global option set '{command.GlobalOptionSetName}' does not exist", ex);
                }
            }
            else
            {
                optionSet.OptionSetType = OptionSetType.Picklist;
                optionSet.IsGlobal = false;
                optionSet.IsCustomOptionSet = true;
                optionSet.IsCustomizable = new BooleanManagedProperty(true);
                optionSet.Name = GetOptionSetName(command.EntityName, attribute.SchemaName); // se non è global. Se è global, il nome è quello del global option set

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
