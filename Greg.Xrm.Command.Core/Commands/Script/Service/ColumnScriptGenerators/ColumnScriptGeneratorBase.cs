using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal abstract class ColumnScriptGeneratorBase(AttributeMetadata field) : IColumnScriptGenerator
	{
		public abstract void GenerateScript(StringBuilder script);

		protected virtual void GenerateBase(StringBuilder script, string commandName)
		{
			script.Append($"pacx column add ");
			script.Append(commandName);
			script.Append(" ");

			script.Append("--table \"");
			script.Append(field.EntityLogicalName);
			script.Append("\" ");

			script.Append("--name \"");
			script.Append(field.DisplayName?.UserLocalizedLabel?.Label);
			script.Append("\" ");

			script.Append("--schemaName \"");
			script.Append(field.SchemaName);
			script.Append("\" ");

			if (!string.IsNullOrWhiteSpace(field.Description?.UserLocalizedLabel?.Label))
			{
				script.Append("--description \"");
				script.Append(field.Description.UserLocalizedLabel.Label);
				script.Append("\" ");
			}

			if (field.RequiredLevel != null && field.RequiredLevel.Value != AttributeRequiredLevel.None)
			{
				script.Append("--requiredLevel ");
				script.Append(field.RequiredLevel);
				script.Append(" ");
			}

			if (field.IsAuditEnabled != null)
			{
				script.Append("--audit ");
				script.Append(field.IsAuditEnabled.Value);
				script.Append(" ");
			}
		}
	}
}
