using Greg.Xrm.Command.Commands.Column.Builders;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public class AttributeMetadataScriptBuilderFactory : IAttributeMetadataScriptBuilderFactory
    {

       private readonly Dictionary<AttributeTypeCode, Func<IAttributeMetadataScriptBuilder>> cache = new();

        public AttributeMetadataScriptBuilderFactory()
        {
            cache.Add(AttributeTypeCode.String, () => new AttributeMetadataScriptBuilderString());
            cache.Add(AttributeTypeCode.Picklist, () => new AttributeMetadataScriptBuilderPicklist());
            cache.Add(AttributeTypeCode.Integer, () => new AttributeMetadataScriptBuilderInteger());
            cache.Add(AttributeTypeCode.Money, () => new AttributeMetadataScriptBuilderMoney());
            cache.Add(AttributeTypeCode.Boolean, () => new AttributeMetadataScriptBuilderBoolean());
            cache.Add(AttributeTypeCode.Decimal, () => new AttributeMetadataScriptBuilderDecimal());
            cache.Add(AttributeTypeCode.Memo, () => new AttributeMetadataScriptBuilderMemo());
            cache.Add(AttributeTypeCode.DateTime, () => new AttributeMetadataScriptBuilderDateTime());
			cache.Add(AttributeTypeCode.Virtual, () => new AttributeMetadataScriptBuilderPicklist());
        }

        public IAttributeMetadataScriptBuilder CreateFor(AttributeTypeCode attributeType)
        {
            if (!cache.TryGetValue(attributeType, out var factory))
                throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The attribute type '{attributeType}' is not supported yet");

            return factory();
        }
    }
}
