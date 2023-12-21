using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public class AttributeMetadataScriptBuilderString : AttributeMetadataScriptBuilderBase
    {
        public override string GetColumnScript(AttributeMetadata attributeMetadata)
        {
            var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
            var attribute = (StringAttributeMetadata)attributeMetadata;

            //sb.Append(CreatePropertyAttribute("text", CommandArgsConstants.TYPE));
            sb.Append(CreatePropertyAttribute(attribute.MaxLength, CommandArgsConstants.MAX_LENGTH));
            sb.Append(CreatePropertyAttribute(attribute.Format, CommandArgsConstants.STRING_FORMAT));
            sb.Append(CreatePropertyAttribute(attribute.AutoNumberFormat, CommandArgsConstants.AUTONUMBER));

            return sb.ToString();
        }
    }
}
