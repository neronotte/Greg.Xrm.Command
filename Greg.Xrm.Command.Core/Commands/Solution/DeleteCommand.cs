using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "delete", HelpText = "Deletes a solution from the current Dataverse environment")]
	public class DeleteCommand
	{
		[Option("uniqueName", "un", HelpText = "The unique name of the solution to delete.")]
		[Required]
		public string? SolutionUniqueName { get; set; }
    }
}
