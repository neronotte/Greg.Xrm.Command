using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForMultiselectPicklist(MultiSelectPicklistAttributeMetadata field)
		: ColumnScriptGeneratorForPicklist(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateScript(script);

			script.Append(" --multiselect");
		}
	}
}
