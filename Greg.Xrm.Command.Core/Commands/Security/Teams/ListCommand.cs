using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	[Command("security", "teams", "list", HelpText = "List teams, optionally filtered by team type and name.")]
	[Alias("security", "team", "list")]
	public class ListCommand : ICanProvideUsageExample
	{
		[Option("type", "t", Order = 1, HelpText = "Owner (0), Access (1), SecurityGroup (2) or Microsoft365Group (3). If omitted, includes all types.")]
		[EnumDataType(typeof(TeamType))]
		public TeamType? Type { get; set; }

		[Option("name", "n", Order = 2, HelpText = "Optional case-insensitive name-contains filter.")]
		public string? Name { get; set; }

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx security teams list", "Powershell");
			writer.WriteCodeBlock("pacx security teams list --type Owner --name Sales", "Powershell");
			writer.WriteParagraph("Types: Owner (0), Access (1), SecurityGroup (2), Microsoft365Group (3). Omit --type to include every type.");
		}
	}
}