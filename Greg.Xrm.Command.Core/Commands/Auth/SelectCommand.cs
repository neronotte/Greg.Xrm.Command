using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Auth
{
	[Command("auth", "select", HelpText = "Selects a connection to use for the next commands")]
	public class SelectCommand
	{
		[Option("name", "n", HelpText = "The name of the authentication profile to set as default.")]
		[Required]
		public string? Name { get; set; }
	}
}
