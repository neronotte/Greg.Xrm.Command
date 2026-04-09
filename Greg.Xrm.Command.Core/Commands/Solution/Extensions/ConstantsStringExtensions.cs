using Microsoft.Xrm.Sdk.Metadata;
using System.Globalization;
using System.Text;

namespace Greg.Xrm.Command.Commands.Solution.Extensions
{
	public static class ConstantsStringExtensions
	{
		public static string RemoveSpecialCharacters(this string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return text;

			var sb = new StringBuilder();
			foreach (char ch in text)
			{
				if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9' || ch == '_')
					sb.Append(ch);
			}
			return sb.ToString();
		}

		public static string RemoveDiacritics(this string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return text;

			var str = text.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder(str.Length);
			for (int i = 0; i < str.Length; i++)
			{
				char ch = str[i];
				if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
					sb.Append(ch);
			}
			return sb.ToString().Normalize(NormalizationForm.FormC);
		}

		public static string? GetGlobalOptionSetName(this OptionSetMetadataBase optionSetMetadata)
		{
			if (optionSetMetadata == null) return null;
			return optionSetMetadata.IsGlobal.HasValue && optionSetMetadata.IsGlobal.Value
				? optionSetMetadata.Name
				: null;
		}
	}
}
