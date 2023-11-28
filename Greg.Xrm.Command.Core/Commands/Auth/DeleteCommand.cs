using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Auth
{
    [Command("auth", "delete", HelpText = "Deletes an authentication profile from the store.")]
	public class DeleteCommand
	{
		[Option("name", "n", HelpText = "The name of the authentication profile to delete.")]
		[Required]
		public string Name { get; set; } = string.Empty;
    }
}
