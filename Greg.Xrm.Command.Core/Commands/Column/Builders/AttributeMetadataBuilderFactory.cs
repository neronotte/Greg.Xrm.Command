using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    public class AttributeMetadataBuilderFactory : IAttributeMetadataBuilderFactory
    {
        private readonly Dictionary<AttributeTypeCode, Func<IAttributeMetadataBuilder>> cache = new();

        public AttributeMetadataBuilderFactory()
        {
            cache.Add(AttributeTypeCode.String, () => new AttributeMetadataBuilderString());
            cache.Add(AttributeTypeCode.Picklist, () => new AttributeMetadataBuilderPicklist());
        }

        public IAttributeMetadataBuilder CreateFor(AttributeTypeCode attributeType)
        {
            if (!cache.TryGetValue(attributeType, out var factory))
                throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The attribute type '{attributeType}' is not supported yet");

            return factory();
        }
    }
}
