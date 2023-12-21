using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    internal class AttributeMetadataScriptBuilderDecimal : AttributeMetadataScriptBuilderBase
	{
		public override string GetColumnScript(AttributeMetadata attributeMetadata)
		{
			var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
			var attribute = (DecimalAttributeMetadata)attributeMetadata;

			if (attribute.Precision.HasValue)
				sb.Append(CreatePropertyAttribute(attribute.Precision.Value, CommandArgsConstants.PRECISION));
			if (attribute.ImeMode.HasValue)
				sb.Append(CreatePropertyAttribute((ImeMode)attribute.ImeMode.Value, CommandArgsConstants.IME_MODE));
			if (attribute.MinValue.HasValue)
				sb.Append(CreatePropertyAttribute(attribute.MinValue.Value, CommandArgsConstants.MIN));
			if (attribute.MaxValue.HasValue)
				sb.Append(CreatePropertyAttribute(attribute.MaxValue.Value, CommandArgsConstants.MAX));

			return sb.ToString();
		}
	}
}
