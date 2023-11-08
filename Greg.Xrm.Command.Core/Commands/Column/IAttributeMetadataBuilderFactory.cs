using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
    public interface IAttributeMetadataBuilderFactory
    {
        IAttributeMetadataBuilder CreateFor(AttributeTypeCode attributeType);
    }
}
