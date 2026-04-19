using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForUniqueIdentifier(UniqueIdentifierAttributeMetadata column) : IColumnScriptGenerator
	{
		public void GenerateScript(StringBuilder script)
		{
			script.Append("# [Primary Key]: " + column.SchemaName);
		}
	}
}
