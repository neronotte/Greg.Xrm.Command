using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForDecimal(DecimalAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "decimal");

			script.Append(" --min ").Append(field.MinValue);
			script.Append(" --max ").Append(field.MaxValue);
			script.Append(" --precision ").Append(field.Precision);
			script.Append(" --imeMode ").Append(field.ImeMode);
		}
	}
}
