using System.Text;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ColumnScriptGeneratorForFile(FileAttributeMetadata field) : ColumnScriptGeneratorBase(field)
	{
		public override void GenerateScript(StringBuilder script)
		{
			base.GenerateBase(script, "file");
			script.Append(" --maxSizeInKB ").Append(field.MaxSizeInKB);
		}
	}
}
