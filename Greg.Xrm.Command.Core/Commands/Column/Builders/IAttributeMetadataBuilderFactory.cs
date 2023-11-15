using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    public interface IAttributeMetadataBuilderFactory
    {
        IAttributeMetadataBuilder CreateFor(AttributeTypeCode attributeType);
    }
}
