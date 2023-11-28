using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Auth
{
    [Command("auth", "rename", HelpText = "Renames an authentication profile")]
	public class RenameCommand
	{
		[Option("old", "o", HelpText = "The new name of the authentication profile")]
		[Required]
		public string OldName { get; set; } = string.Empty;

		[Option("new", "n", HelpText = "The new name of the authentication profile")]
		[Required]
        public string NewName { get; set; } = string.Empty;
	}
}
