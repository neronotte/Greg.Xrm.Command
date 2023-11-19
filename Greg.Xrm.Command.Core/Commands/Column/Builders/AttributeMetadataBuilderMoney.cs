using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
	internal class AttributeMetadataBuilderMoney : AttributeMetadataBuilderNumericBase
    {

        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new MoneyAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            // Set extended properties
            attribute.MinValue = GetDoubleValue(command.MinValue, Limit.Min); 
            attribute.MaxValue = GetDoubleValue(command.MaxValue, Limit.Max);

            attribute.Precision = command.Precision; //1;
            attribute.PrecisionSource = command.PrecisionSource; // default 2;
            attribute.ImeMode = command.ImeMode; // ImeMode.Disabled;

            if (attribute.PrecisionSource == 0 && attribute.Precision is null)
            { 
                throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The attribute 'Precision' must be specified when PrecisionSource = 0");
            }


            return Task.FromResult((AttributeMetadata)attribute);
        }
    }
}
