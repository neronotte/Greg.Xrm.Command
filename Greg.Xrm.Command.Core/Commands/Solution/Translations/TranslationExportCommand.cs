namespace Greg.Xrm.Command.Commands.Solution.Translations
{
	[Command("solution", "translation", "export", HelpText = "Exports a Dataverse translation package for a solution and optionally unpacks it.")]
	public class TranslationExportCommand
	{
		[Option("solution", "s", Order = 1, HelpText = "The unique name of the solution whose translations should be exported. Uses the current default solution if omitted.")]
		public string? SolutionName { get; set; }

		[Option("path", "p", Order = 2, HelpText = "The path of the .zip file to create. Defaults to the current folder.")]
		public string? Path { get; set; }

		[Option("unpack", "u", Order = 3, DefaultValue = false, HelpText = "Unpacks the exported zip file into a folder next to the archive.")]
		public bool Unpack { get; set; }
	}
}
