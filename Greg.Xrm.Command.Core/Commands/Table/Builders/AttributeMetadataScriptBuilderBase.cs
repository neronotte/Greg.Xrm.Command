using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
	public abstract class AttributeMetadataScriptBuilderBase : IAttributeMetadataScriptBuilder
	{
		public readonly Dictionary<string, string> argumentsDictionary = new();

		public abstract string GetColumnScript(AttributeMetadata attributeMetadata);

		public AttributeMetadataScriptBuilderBase()
		{
			argumentsDictionary = CommandArgsConstants.GetArgumentDictionary();
		}

		public string GetCommonColumns(AttributeMetadata attributeMetadata, string? typeName = null)
		{
			var sb = new StringBuilder();
			var typeString = typeName ?? attributeMetadata?.AttributeType.Value.ToString();
			sb.Append(CommandArgsConstants.COLUMN_COMMAND);

			//table i.Space(f)o
			sb.Append(CreatePropertyAttribute(attributeMetadata.EntityLogicalName, CommandArgsConstants.TABLE));
			sb.Append(CreatePropertyAttribute(typeString, CommandArgsConstants.TYPE));

			//common description
			sb.Append(CreatePropertyAttribute(attributeMetadata.Description, CommandArgsConstants.DESCRIPTION));
			sb.Append(CreatePropertyAttribute(attributeMetadata.DisplayName, CommandArgsConstants.NAME));
			sb.Append(CreatePropertyAttribute(attributeMetadata.SchemaName, CommandArgsConstants.SCHEMA_NAME));
			sb.Append(CreatePropertyAttribute(attributeMetadata.RequiredLevel, CommandArgsConstants.REQUIRED_LEVEL));
			sb.Append(CreatePropertyAttribute(attributeMetadata.IsAuditEnabled, CommandArgsConstants.AUDIT, true));

			return sb.ToString();
		}


		public string CreatePropertyAttribute<T>(T attributeMetadata, string dictKey, object defaultValue = null)
		{
			var result = String.Empty;
			if (attributeMetadata == null)
				return result;

			if (!argumentsDictionary.TryGetValue(dictKey, out var shortArg))
				throw new ArgumentNullException($"The key {dictKey} is not supported for this type");

			if (attributeMetadata is string strProp)
			{
				if (!string.IsNullOrWhiteSpace(strProp?.ToString()))
					return $"-{shortArg} {WrapString(strProp.ToString())}".AddSpace();
			}
			if (attributeMetadata is Label labProp) 
			{
				if (!string.IsNullOrWhiteSpace(labProp?.UserLocalizedLabel?.Label))
					return $"-{shortArg} {WrapString(labProp.UserLocalizedLabel.Label)}".AddSpace();
				return result;
			}
			if (attributeMetadata is AttributeRequiredLevelManagedProperty arlProp)
			{
				if (arlProp != null && arlProp.Value != AttributeRequiredLevel.None)
					return $"-{shortArg} {arlProp.Value}".AddSpace();
				return result;
			}
			if (attributeMetadata is BooleanManagedProperty bProp)
			{
				if (bProp.Value != (bool)defaultValue)
					return $"-{shortArg} {bProp.Value}".AddSpace();
				return result;
			}
			if (typeof(int?).IsAssignableFrom(typeof(T)))
			{
				return $"-{shortArg} {attributeMetadata}".AddSpace();
			}
			if (typeof(decimal?).IsAssignableFrom(typeof(T)))
			{
				return $"-{shortArg} {WrapNumber(Convert.ToDecimal(attributeMetadata))}".AddSpace();
			}
			if (typeof(bool?).IsAssignableFrom(typeof(T)))
			{
				var bAttr = Convert.ToBoolean(attributeMetadata);
				if (defaultValue != null && bAttr != ((bool?)defaultValue).Value)
					return $"-{shortArg} {attributeMetadata}".AddSpace();
			}
			if (attributeMetadata is DateTimeBehavior dtProp)
			{
				if (dtProp.Value != (DateTimeBehavior)defaultValue)
					return $"-{shortArg} {(DateTimeBehavior)dtProp?.Value}".AddSpace();
				return result;
			}
			if (attributeMetadata is OwnershipTypes ownProp)
			{
				if (ownProp != (OwnershipTypes)defaultValue)
					return $"-{shortArg} {(OwnershipTypes)ownProp}".AddSpace();
				return result;
			}
			return result;

		}

		#region Format Utilities
		//public string Space(string text)
		//{
		//	return text + (string.IsNullOrWhiteSpace(text) ? String.Empty : " ");
		//}
		public string WrapString(string? str)
		{
			return (String.IsNullOrEmpty(str) || str.Contains(' ')) ? $"\"{str}\"" : str;
		}
		public string WrapNumber(decimal str)
		{
			return str.ToString().Contains('.') ? $"\"{str}\"" : str.ToString();
		}

		#endregion
	}
}
