using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    public class AttributeMetadataBuilderFactory : IAttributeMetadataBuilderFactory
    {
        private readonly Dictionary<AttributeTypeCode, Func<IAttributeMetadataBuilder>> cache = new();

        public AttributeMetadataBuilderFactory()
        {
            cache.Add(AttributeTypeCode.String, () => new AttributeMetadataBuilderString());
            cache.Add(AttributeTypeCode.Integer, () => new AttributeMetadataBuilderInteger());
			cache.Add(AttributeTypeCode.Decimal, () => new AttributeMetadataBuilderDecimal());
			cache.Add(AttributeTypeCode.Double, () => new AttributeMetadataBuilderDouble());
			cache.Add(AttributeTypeCode.Boolean, () => new AttributeMetadataBuilderBoolean());
			cache.Add(AttributeTypeCode.Picklist, () => new AttributeMetadataBuilderPicklist());
			cache.Add(AttributeTypeCode.Money, () => new AttributeMetadataBuilderMoney());
			cache.Add(AttributeTypeCode.Memo, () => new AttributeMetadataBuilderMemo());
            cache.Add(AttributeTypeCode.DateTime, () => new AttributeMetadataBuilderDateTime());
        }

        public IAttributeMetadataBuilder CreateFor(AttributeTypeCode attributeType)
        {
            if (!cache.TryGetValue(attributeType, out var factory))
                throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The attribute type '{attributeType}' is not supported yet");

            return factory();
        }
    }
}
