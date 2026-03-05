using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Org.Language
{
	[Command("org", "language", "remove", HelpText = "Deprovisions a language from the organization")]
	[Alias("org", "language", "deprovision")]
	[Alias("org", "lang", "remove")]
	[Alias("org", "lang", "deprovision")]
	public class DeprovisionLanguageCommand
	{
		[Option("lcid", "l", HelpText = "The LCID of the language to provision")]
		[Required]
		public int LanguageCode { get; set; }
	}
}
