using System.Text;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal interface IColumnScriptGenerator
	{
		void GenerateScript(StringBuilder script);
	}
}
