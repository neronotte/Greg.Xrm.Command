using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Auth
{
	[Command("auth", "create", HelpText = "Create and store authentication profiles on this computer")]
	public class CreateCommand
	{
		[Option("name", "n", HelpText = "The name you want to give to this authentication profile (maximum 30 characters).")]
		[Required]
		public string? Name { get; set; }

		[Option("conn", "cs", HelpText = "The connection string that will be used to connect to the dataverse. If not provided, the login prompt will be displayed.")]
		[Required]
		public string? ConnectionString { get; set; }
	}
}
