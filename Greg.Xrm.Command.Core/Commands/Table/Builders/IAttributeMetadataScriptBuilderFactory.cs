using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public interface IAttributeMetadataScriptBuilderFactory
    {
        IAttributeMetadataScriptBuilder CreateFor(AttributeTypeCode attributeType);

    }
}
