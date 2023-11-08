using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;

namespace Greg.Xrm.Command
{
	public static class Extensions
	{
		public static string OnlyLettersNumbersOrUnderscore(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;

			var sb = new StringBuilder();
			foreach (var c in text)
			{
				if (char.IsLetterOrDigit(c) || c == '_')
				{
					sb.Append(c.ToString().ToLowerInvariant());
				}
			}
			return sb.ToString();
		}
	}
}
