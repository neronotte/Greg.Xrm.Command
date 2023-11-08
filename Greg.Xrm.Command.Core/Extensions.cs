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

		public static string Left(this string? text, int len)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			if (text.Length <= len) return text;
			return text.Substring(0, len);
		}

		public static bool IsOnlyLowercaseLettersOrNumbers(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return false;

			foreach (var c in text)
			{
				if (!char.IsLetterOrDigit(c)) return false;
				if (char.IsLetter(c) && char.IsUpper(c)) return false;
			}
			return true;
		}


		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				return value;
			}
			return default;
		}
	}
}
