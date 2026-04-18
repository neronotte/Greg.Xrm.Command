using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "file", HelpText = "Creates a file column.")]
	public class CreateFileCommand : BaseCreateCommand, ICanProvideUsageExample
	{
		[Option("maxSizeInKB", "maxKb", HelpText = "For File or Image type columns indicates the maximum size in KB for the column. Do not provide a value if you want to stay with the default (32Mb for file columns, 10Mb for image columns). The value must be lower than 10485760 (1Gb) for file columns, and lower than 30720 (30Mb) for image columns.")]
		public int? MaxSizeInKB { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock(@"# Creates a simple file column
pacx column create --type File -t tableName -n columnName

# specifies the max allowed size in KB (10 MB)
pacx column create --type File -t tableName -n columnName --maxSizeInKB 10240
pacx column create --type File -t tableName -n columnName -maxKb 10240
", "Powershell");
		}
	}
}
