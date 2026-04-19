using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForUnsupportedType(AttributeMetadata column) : IColumnScriptGenerator
	{
		public void GenerateScript(StringBuilder script)
		{
			script.Append($"# Column '{column.LogicalName}' of type '{column.AttributeType}' is not supported by the script generator.");
		}
	}
}
