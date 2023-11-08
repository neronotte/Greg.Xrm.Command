using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
    public interface IAttributeMetadataBuilder
    {
        AttributeMetadata CreateFrom(CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix);
    }
}
