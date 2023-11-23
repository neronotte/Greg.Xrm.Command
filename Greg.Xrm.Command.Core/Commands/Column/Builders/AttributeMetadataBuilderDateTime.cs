using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    internal class AttributeMetadataBuilderDateTime : AttributeMetadataBuilderBase
    {
        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            
            var attribute = new DateTimeAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            attribute.Format = command.DateTimeFormat; // DateTimeFormat.DateOnly;
            attribute.ImeMode = command.ImeMode; // ImeMode.Disabled;

            return Task.FromResult((AttributeMetadata)attribute);
        }
    }
}
