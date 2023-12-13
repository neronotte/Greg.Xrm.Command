using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Relationship
{
	[Command("rel", "delete", HelpText = "Deletes a relationship")]
	public class DeleteCommand
	{
        [Option("name", "n", HelpText = "The schema name of the relationship")]
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
