using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[Command("security", "roles", "list", HelpText = "List the security roles defined in the environment (one row per role, business unit copies are excluded).")]
	[Alias("security", "role", "list")]
	public class ListCommand : ICanProvideUsageExample
	{
		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("List all security roles defined in the environment. Dataverse creates a copy of each role for every business unit: only the root role is shown.");
			writer.WriteCodeBlock("pacx security roles list", "Powershell");

			writer.WriteParagraph("List only the unmanaged roles (roles created or customized in this environment, or imported via unmanaged solutions), excluding the ones coming from managed solutions:");
			writer.WriteCodeBlock("pacx security roles list --unmanaged-only", "Powershell");
			writer.WriteCodeBlock("pacx security roles list -um", "Powershell");

			writer.WriteParagraph("List only the roles whose name contains a given text (case insensitive):");
			writer.WriteCodeBlock("pacx security roles list --name sales", "Powershell");
			writer.WriteCodeBlock("pacx security roles list -n \"sales\" -um", "Powershell");
		}

		[Option("name", "n", Order = 1, HelpText = "If specified, only the roles whose name contains the given text are returned.")]
		public string? Name { get; set; }

		[Option("unmanaged-only", "um", Order = 2, DefaultValue = false, HelpText = "If specified, only unmanaged roles are returned.")]
		public bool UnmanagedOnly { get; set; }
	}
}
