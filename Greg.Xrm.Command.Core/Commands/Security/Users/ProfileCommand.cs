using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	public enum ProfileOutputFormat { Tree, Json }

	[Command("user", "profile", HelpText = "Show a user's business unit, direct and inherited roles, teams and their roles as a tree or JSON.")]
	[Alias("users", "profile")]
	[Alias("security", "user", "profile")]
	[Alias("security", "users", "profile")]
	public class ProfileCommand : ICanProvideUsageExample
	{
		[Option("user", "u", Order = 1, HelpText = "User GUID, domain name or primary email. If omitted, uses the current connected user.")]
		public string? User { get; set; }

		[Option("format", "f", Order = 2, DefaultValue = ProfileOutputFormat.Tree, HelpText = "Tree or Json. Defaults to Tree. Use --nologo for JSON pipelines.")]
		[EnumDataType(typeof(ProfileOutputFormat))]
		public ProfileOutputFormat Format { get; set; } = ProfileOutputFormat.Tree;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteCodeBlock("pacx user profile", "Powershell");
			writer.WriteCodeBlock("pacx user profile --user john.doe@contoso.com --format Tree", "Powershell");
			writer.WriteCodeBlock("pacx user profile -u john.doe@contoso.com -f Json --nologo", "Powershell");
			writer.WriteParagraph("Roles include Direct and Team sources. Each membership team lists its assigned roles, including teams with no roles. Microsoft Entra group-team membership reflects Dataverse synchronization; users who have never accessed the environment may have incomplete membership information.");
		}
	}
}