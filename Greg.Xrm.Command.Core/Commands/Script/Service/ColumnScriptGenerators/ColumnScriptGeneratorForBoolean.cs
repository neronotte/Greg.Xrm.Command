using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForBoolean(BooleanAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "boolean");

			script.Append($" --trueLabel ");
			script.Append(field.OptionSet.TrueOption?.Label?.UserLocalizedLabel?.Label ?? "\"\"");
			script.Append($" --falseLabel ");
			script.Append(field.OptionSet.FalseOption?.Label?.UserLocalizedLabel?.Label ?? "\"\"");
		}
	}
}
