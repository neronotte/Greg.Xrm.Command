using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public class AttributeMetadataScriptBuilderDateTime : AttributeMetadataScriptBuilderBase
    {
        public override string GetColumnScript(AttributeMetadata attributeMetadata)
        {

            var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
            var attribute = (DateTimeAttributeMetadata)attributeMetadata;

            sb.Append(CreatePropertyAttribute(attribute.DateTimeBehavior.Value, CommandArgsConstants.DATETIME_BEHAVIOR));
            if(attribute.Format.HasValue)
                sb.Append(CreatePropertyAttribute(((DateTimeFormat)attribute.Format).ToString(), CommandArgsConstants.DATETIME_FORMAT));
            if(attribute.ImeMode.HasValue)
                sb.Append(CreatePropertyAttribute(((ImeMode)attribute.ImeMode).ToString(), CommandArgsConstants.IME_MODE));

            return sb.ToString();
        }


    }
}
