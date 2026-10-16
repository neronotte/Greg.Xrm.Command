using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	[Command("solution", "translation", "import", HelpText = "Imports a Dataverse translation package from a zip file or from a repacked folder.")]
	public class TranslationImportCommand
	{
		[Option("path", "p", Order = 1, HelpText = "The path of the translation .zip file or the folder containing the translation payload.")]
		[Required]
		public string? Path { get; set; }

		[Option("repack", "r", Order = 2, DefaultValue = false, HelpText = "When a folder is specified, create a .zip from its content before importing it.")]
		public bool Repack { get; set; }

		[Option("nowait", "nw", Order = 3, DefaultValue = false, HelpText = "Returns immediately after starting the import job instead of waiting for its completion.")]
		public bool NoWait { get; set; }
	}
}
