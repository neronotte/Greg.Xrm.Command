using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForInteger(IntegerAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "integer");
			script.Append(" --min ").Append(field.MinValue);
			script.Append(" --max ").Append(field.MaxValue);
			if (field.Format != null)
			{
				script.Append(" --intFormat ").Append(field.Format);
			}
		}
	}
}
