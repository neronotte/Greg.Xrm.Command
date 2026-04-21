using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	[Command("usersettings", "set", HelpText = "Sets a user setting property for the specified or currently logged-in user.")]
	public class SetCommand : ICanProvideUsageExample
	{
		[Option("user", "u", Order = 1, HelpText = "Domain name of the user whose settings to update (e.g. DOMAIN\\john.doe). If omitted, the current user's settings are updated.")]
		public string? UserDomainName { get; set; }

		[Option("key", "k", Order = 2, HelpText = "The usersettings field name to update (e.g. uilanguageid, timezonecode, showweeknumber). Run 'pacx usersettings set --help' to list all supported fields.")]
		[Required]
		public string Key { get; set; } = string.Empty;

		[Option("value", "v", Order = 3, HelpText = "The value to assign to the setting. The format depends on the field type (integer, boolean, LCID, etc.).")]
		[Required]
		public string Value { get; set; } = string.Empty;

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set the UI language for the current user to Italian (LCID 1040):");
			writer.WriteCodeBlock("pacx usersettings set --key uilanguageid --value 1040", "Powershell");

			writer.WriteParagraph("Set the time zone for another user (by domain name):");
			writer.WriteCodeBlock("pacx usersettings set --user DOMAIN\\\\john.doe --key timezonecode --value 85", "Powershell");

			writer.WriteParagraph("Switch to 24-hour time format for the current user:");
			writer.WriteCodeBlock("pacx usersettings set --key timeformatcode --value 1", "Powershell");

			writer.WriteParagraph("Show week numbers in the calendar:");
			writer.WriteCodeBlock("pacx usersettings set --key showweeknumber --value true", "Powershell");

			writer.WriteParagraph("Supported keys:");
			writer.WriteTable(
				UserSettingRegistry.Fields.Values.OrderBy(d => d.FieldName).ToList(),
				["Key", "Display Name", "Type", "Description"],
				d =>
				[
					d.FieldName,
					d.DisplayName,
					d.FieldType.ToString(),
					d.HelpText
				]);
		}
	}
}
