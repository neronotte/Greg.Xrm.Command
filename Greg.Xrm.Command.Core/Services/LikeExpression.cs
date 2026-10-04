namespace Greg.Xrm.Command.Services
{
	public static class LikeExpression
	{
		/// <summary>
		/// Escapes the LIKE wildcard characters ([, %, _) so that they are matched literally.
		/// </summary>
		public static string Escape(string value)
		{
			return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
		}

		/// <summary>
		/// Returns a LIKE pattern that matches values containing <paramref name="value"/> (trimmed and escaped).
		/// </summary>
		public static string Contains(string value)
		{
			return $"%{Escape(value.Trim())}%";
		}
	}
}
