using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForString(StringAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			GenerateBase(script, "string");

			if (field.Format != null)
			{
				script.Append("--format \"");
				script.Append(field.Format);
				script.Append("\" ");
			}
			if (field.MaxLength != null)
			{
				script.Append("--len ");
				script.Append(field.MaxLength);
				script.Append(" ");
			}
			if (field.AutoNumberFormat != null)
			{
				script.Append("--autoNumber \"");
				script.Append(field.AutoNumberFormat);
				script.Append("\" ");
			}
		}
	}
}
