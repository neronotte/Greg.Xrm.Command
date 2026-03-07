namespace Greg.Xrm.Command.Commands.Column.Create
{
	[Command("column", "add", "file", HelpText = "Creates a file column.")]
	public class CreateFileCommand : BaseCreateCommand
	{
		[Option("maxSizeInKB", "maxKb", HelpText = "For File or Image type columns indicates the maximum size in KB for the column. Do not provide a value if you want to stay with the default (32Mb for file columns, 10Mb for image columns). The value must be lower than 10485760 (1Gb) for file columns, and lower than 30720 (30Mb) for image columns.")]
		public int? MaxSizeInKB { get; set; }
	}
}
