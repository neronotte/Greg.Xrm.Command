using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    internal class AttributeMetadataScriptBuilderPicklist : AttributeMetadataScriptBuilderBase
	{
		public override string GetColumnScript(AttributeMetadata attributeMetadata)
		{
			//Check multiselect
			EnumAttributeMetadata attr = attributeMetadata.AttributeType.Value == AttributeTypeCode.Picklist ? (PicklistAttributeMetadata)attributeMetadata :  (MultiSelectPicklistAttributeMetadata)attributeMetadata;

			var sb = new StringBuilder(GetCommonColumns(attributeMetadata, AttributeTypeCode.Picklist.ToString()));

			sb.Append(CreatePropertyAttribute(attr.OptionSet.IsGlobal, CommandArgsConstants.GLOBAL_OPTIONSET_NAME));
			sb.Append(CreatePropertyAttribute(string.Join(",", attr.OptionSet.Options.Select(x => x.Label.UserLocalizedLabel.Label).ToArray()), CommandArgsConstants.OPTIONS));

			if (attr is MultiSelectPicklistAttributeMetadata)
				sb.Append(CreatePropertyAttribute(true, CommandArgsConstants.MULTISELECT));

			return sb.ToString();
		}
	}
}
