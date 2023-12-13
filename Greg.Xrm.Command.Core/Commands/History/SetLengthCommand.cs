using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.History
{
	[Command("history", "setLength", HelpText = "Allows to specify the length of the command history that will be persisted.")]
	[Alias("history", "set-length")]
	[Alias("history", "len")]
	public class SetLengthCommand
	{
		[Option("length", "l", HelpText = "The maximum number of commands to keep in history.")]
		[Required]
		[Range(1, 100, ErrorMessage = "The length must be between 1 and 10000")]
		public int Length { get; set; }
	}
}
