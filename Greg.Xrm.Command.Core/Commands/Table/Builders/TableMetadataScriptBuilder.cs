using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
	internal class TableMetadataScriptBuilder : AttributeMetadataScriptBuilderBase
	{
		public override string GetColumnScript(AttributeMetadata attributeMetadata)
		{
			var sb = new StringBuilder();
			//PRoprietà di default + queste:

			sb.Append(CreatePropertyAttribute(attributeMetadata.Description, CommandArgsConstants.PRIMARY_ATTRIBUTE_DESCRIPTION));
			sb.Append(CreatePropertyAttribute(attributeMetadata.AutoNumberFormat, CommandArgsConstants.PRIMARY_ATTRIBUTE_AUTONUMBER_FORMAT));
			sb.Append(CreatePropertyAttribute(attributeMetadata.RequiredLevel, CommandArgsConstants.PRIMARY_ATTRIBUTE_REQUIRED_LEVEL));

			if (attributeMetadata.AttributeType == AttributeTypeCode.String)
				sb.Append(CreatePropertyAttribute(((StringAttributeMetadata)attributeMetadata).MaxLength, CommandArgsConstants.PRIMARY_ATTRIBUTE_MAX_LENGTH));

			sb.AppendLine("");
			return sb.ToString();

		}

		public string GetTableScript(EntityMetadata entityMetadata)
		{
			var sb = new StringBuilder();
			sb.Append(CommandArgsConstants.TABLE_COMMAND);
            sb.Append(CreatePropertyAttribute(entityMetadata.DisplayName, CommandArgsConstants.NAME));
			sb.Append(CreatePropertyAttribute(entityMetadata.SchemaName, CommandArgsConstants.SCHEMA_NAME));
			if (entityMetadata.Description?.UserLocalizedLabel?.Label != null)
				sb.Append(CreatePropertyAttribute(entityMetadata.Description, CommandArgsConstants.DESCRIPTION));
			sb.Append(CreatePropertyAttribute(entityMetadata.IsAuditEnabled?.Value, CommandArgsConstants.AUDIT, true));

			if (entityMetadata.DisplayCollectionName?.UserLocalizedLabel.Label != null)
				sb.Append(CreatePropertyAttribute(entityMetadata.DisplayCollectionName, CommandArgsConstants.PLURAL));
			sb.Append(CreatePropertyAttribute(entityMetadata.OwnershipType, CommandArgsConstants.OWNERSHIP, OwnershipTypes.UserOwned));
			sb.Append(CreatePropertyAttribute(entityMetadata.IsActivity, CommandArgsConstants.IS_ACTIVITY, false));
			sb.Append(CreatePropertyAttribute(entityMetadata.PrimaryNameAttribute, CommandArgsConstants.PRIMARY_ATTRIBUTE_SCHEMA_NAME));

			//sb.AppendLine("");
			return sb.ToString();
		}
	}
}
