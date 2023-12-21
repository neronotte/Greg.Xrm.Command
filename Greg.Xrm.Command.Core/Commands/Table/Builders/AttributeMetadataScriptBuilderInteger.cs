using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    internal class AttributeMetadataScriptBuilderInteger : AttributeMetadataScriptBuilderBase
    {
        public override string GetColumnScript(AttributeMetadata attributeMetadata)
        {

            var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
            var attribute = (IntegerAttributeMetadata)attributeMetadata;

            //sb.Append(CreatePropertyAttribute("integer", CommandArgsConstants.TYPE));
            if(attribute.Format.HasValue)
                sb.Append(CreatePropertyAttribute(((IntegerFormat)attribute.Format.Value).ToString(), CommandArgsConstants.INT_FORMAT));
            if(attribute.MinValue.HasValue)
                sb.Append(CreatePropertyAttribute(attribute.MinValue.Value, CommandArgsConstants.MIN));
            if(attribute.MaxValue.HasValue)
                sb.Append(CreatePropertyAttribute(attribute.MaxValue.Value, CommandArgsConstants.MAX));

            return sb.ToString();
        }
    }
}
