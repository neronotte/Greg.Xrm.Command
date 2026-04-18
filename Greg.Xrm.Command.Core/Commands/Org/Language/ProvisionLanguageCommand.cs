using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Org.Language
{
	[Command("org", "language", "add", HelpText = "Provision a new language for the organization")]
	[Alias("org", "language", "provision")]
	[Alias("org", "lang", "add")]
	[Alias("org", "lang", "provision")]
	public class ProvisionLanguageCommand
	{
		[Option("lcid", "l", HelpText = "The LCID of the language to provision")]
		[Required]
		public int LanguageCode { get; set; }
	}
}
