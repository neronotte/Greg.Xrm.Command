using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	/// <summary>
	/// On environments where the language of the current user differs from the
	/// language the labels have been authored in, UserLocalizedLabel can be null.
	/// Picks the best label available instead of failing.
	/// </summary>
	public static class LabelExtensions
	{
		private const int English = 1033;

		public static string GetTextOrDefault(this Label? label, string fallback)
		{
			return label?.UserLocalizedLabel?.Label
				?? label?.LocalizedLabels?.FirstOrDefault(l => l.LanguageCode == English && !string.IsNullOrWhiteSpace(l.Label))?.Label
				?? label?.LocalizedLabels?.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.Label))?.Label
				?? fallback;
		}
	}
}
