using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    internal class AttributeMetadataBuilderInteger : AttributeMetadataBuilderNumericBase
    {

        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new IntegerAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            attribute.MinValue = GetIntValue(command.MinValue, Limit.Min);
            attribute.MaxValue = GetIntValue(command.MaxValue, Limit.Max);
            attribute.Format = command.IntegerFormat;

            return Task.FromResult((AttributeMetadata)attribute);
        }
    }
}
