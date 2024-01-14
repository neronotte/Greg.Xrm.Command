using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    internal class AttributeMetadataScriptBuilderMemo : AttributeMetadataScriptBuilderBase
    {
        public override string GetColumnScript(AttributeMetadata attributeMetadata)
        {

            var sb = new StringBuilder(GetCommonColumns(attributeMetadata));
            var attribute = (MemoAttributeMetadata)attributeMetadata;

            sb.Append(CreatePropertyAttribute(attribute.Format, CommandArgsConstants.STRING_FORMAT));
            sb.Append(CreatePropertyAttribute(attribute.FormatName, CommandArgsConstants.MEMO_FORMAT));
            sb.Append(CreatePropertyAttribute(attribute.MaxLength, CommandArgsConstants.MAX_LENGTH));
            if(attribute.ImeMode.HasValue)
                 sb.Append(CreatePropertyAttribute(attribute.ImeMode.Value.ToString(), CommandArgsConstants.IME_MODE));

            return sb.ToString();
        }
    }
}
