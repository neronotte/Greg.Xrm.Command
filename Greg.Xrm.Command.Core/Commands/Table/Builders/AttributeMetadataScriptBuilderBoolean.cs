using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public class AttributeMetadataScriptBuilderBoolean : AttributeMetadataScriptBuilderBase
    {
        public override string GetColumnScript(AttributeMetadata attributeMetadata)
        {

            var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
            var attribute = (BooleanAttributeMetadata)attributeMetadata;

            sb.Append(CreatePropertyAttribute(attribute.OptionSet.TrueOption, CommandArgsConstants.TRUE_LABEL));
            sb.Append(CreatePropertyAttribute(attribute.OptionSet.FalseOption, CommandArgsConstants.FALSE_LABEL));

            return sb.ToString();
        }
    }
}
