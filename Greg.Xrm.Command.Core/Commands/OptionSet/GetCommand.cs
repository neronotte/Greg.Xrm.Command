using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	[Command("optionset", "get", HelpText = "Returns the definition of an existing global option set from the Dataverse environment.")]
	public class GetCommand
	{
		[Option("name", "n", "The schema name of the global option set to retrieve.")]
		[Required]
		public string Name { get; set; } = string.Empty;
	}
}
