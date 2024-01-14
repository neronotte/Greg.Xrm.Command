using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    internal class AttributeMetadataScriptBuilderMoney : AttributeMetadataScriptBuilderBase
    {
        public override string GetColumnScript(AttributeMetadata attributeMetadata)
        {

            var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
            var attribute = (MoneyAttributeMetadata)attributeMetadata;

            //sb.Append(CreatePropertyAttribute("money", CommandArgsConstants.TYPE));
            if (attribute.MinValue.HasValue)
                sb.Append(CreatePropertyAttribute<double>(attribute.MinValue.Value, CommandArgsConstants.MIN));
            if (attribute.MaxValue.HasValue)
                sb.Append(CreatePropertyAttribute<double>(attribute.MaxValue.Value, CommandArgsConstants.MAX));
            if (attribute.Precision.HasValue)
                sb.Append(CreatePropertyAttribute<Int32>(attribute.Precision.Value, CommandArgsConstants.PRECISION));
            if (attribute.PrecisionSource.HasValue)
                sb.Append(CreatePropertyAttribute<Int32>(attribute.PrecisionSource.Value, CommandArgsConstants.PRECISION_SOURCE));
            if (attribute.ImeMode.HasValue)
                sb.Append(CreatePropertyAttribute(((ImeMode)attribute.ImeMode.Value).ToString(), CommandArgsConstants.IME_MODE));

            return sb.ToString();
        }
    }
}
