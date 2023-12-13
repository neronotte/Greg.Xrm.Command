using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.History
{
    [Command("history", "get", HelpText = "Get the list of commands executed in the past")]
	[Alias("get-history")]
	[Alias("get", "history")]
    public class GetCommand
	{
		[Option("length", "l", HelpText = "The number of commands to retrieve. If not specified, retrieves the whole command list.")]
		[Range(1, 10000)]
        public int? Length { get; set; }

		[Option("file", "f", HelpText = "If you want to save the command list to a specific file, specify the name of the file.")]
		public string? File { get; set; }
    }
}
