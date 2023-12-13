using Greg.Xrm.Command.Parsing;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Solution
{
	[Command("solution", "create", HelpText = "Creates a new unmanaged solution in the current Dataverse environment,\nalso creating the publisher, if needed.")]
	public class CreateCommand
	{
		[Option("name", "n", HelpText = "The display name of the solution to create")]
		[Required]
		public string? DisplayName { get; set; }

		[Option("uniqueName", "un", HelpText = "The unique name of the solution to create. If not specified, is deducted from the display name")]
		public string? UniqueName { get; set; }

		[Option("publisherUniqueName", "pun", HelpText = "The unique name of the publisher to create. If not specified, is deducted from the friendly name or customization prefix")]
		public string? PublisherUniqueName { get; set; }

		[Option("publisherFriendlyName", "puf", HelpText = "The friendly name of the publisher to create. If not specified, is deducted from the unique name or customization prefix")]
		public string? PublisherFriendlyName { get; set; }

		[Option("publisherPrefix", "pp", HelpText = "The customization prefix of the publisher to create. If not specified, is deducted from the unique name.")]
		public string? PublisherCustomizationPrefix { get; set; }

		[Option("publisherOptionSetPrefix", "pop", HelpText = "The option set prefix of the publisher to create (5 digit number).")]
		public int? PublisherOptionSetPrefix { get; set; }
    }
}
