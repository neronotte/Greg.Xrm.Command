using Greg.Xrm.Command.Services.OptionSet;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    public class AttributeMetadataBuilderFactory : IAttributeMetadataBuilderFactory
    {
        private readonly Dictionary<SupportedAttributeType, Func<IAttributeMetadataBuilder>> cache = [];

        public AttributeMetadataBuilderFactory(IOutput output, IOptionSetParser optionSetParser)
        {
            cache.Add(SupportedAttributeType.String, () => new AttributeMetadataBuilderString());
            cache.Add(SupportedAttributeType.Integer, () => new AttributeMetadataBuilderInteger());
			cache.Add(SupportedAttributeType.Decimal, () => new AttributeMetadataBuilderDecimal());
			cache.Add(SupportedAttributeType.Double, () => new AttributeMetadataBuilderDouble());
			cache.Add(SupportedAttributeType.Boolean, () => new AttributeMetadataBuilderBoolean());
			cache.Add(SupportedAttributeType.Picklist, () => new AttributeMetadataBuilderPicklist(output, optionSetParser));
			cache.Add(SupportedAttributeType.Money, () => new AttributeMetadataBuilderMoney());
			cache.Add(SupportedAttributeType.Memo, () => new AttributeMetadataBuilderMemo());
			cache.Add(SupportedAttributeType.DateTime, () => new AttributeMetadataBuilderDateTime());
			cache.Add(SupportedAttributeType.File, () => new AttributeMetadataBuilderFile());
			cache.Add(SupportedAttributeType.Image, () => new AttributeMetadataBuilderImage());
		}

        public IAttributeMetadataBuilder CreateFor(SupportedAttributeType attributeType)
        {
            if (!cache.TryGetValue(attributeType, out var factory))
                throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The attribute type '{attributeType}' is not supported yet");

            return factory();
        }
    }
}
