using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForMemo(MemoAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			GenerateBase(script, "string");

			if (field.Format != null)
			{
				script.Append("--memoFormat \"");
				script.Append(field.Format);
				script.Append("\" ");
			}
			if (field.MaxLength != null)
			{
				script.Append("--len ");
				script.Append(field.MaxLength);
				script.Append(" ");
			}
			if (field.ImeMode != null)
			{
				script.Append("--imeMode \"");
				script.Append(field.ImeMode);
				script.Append("\" ");
			}
		}
	}
}
