using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
	public class AttributeMetadataScriptBuilderEmpty : IAttributeMetadataScriptBuilder
	{
		public string GetColumnScript(AttributeMetadata attributeMetadata)
		{
            var sb = new StringBuilder();

            sb.Append("# Column ");
            sb.Append(attributeMetadata.LogicalName);
            sb.Append(" is of type ");
            sb.Append(attributeMetadata.AttributeType.ToString());
            sb.AppendLine(" and is not yet supported by the script builder, you need to generate it manually.");

			return sb.ToString();
		}
	}
}
