namespace Greg.Xrm.Command.Model
{
	public static class ColorExtensions
	{
		const string ValidEx = "0123456789ABCDEF";


		public static bool IsValidExadecimalColor(this string? color)
		{
			if (string.IsNullOrWhiteSpace(color))
			{
				return false;
			}

			if (color.StartsWith('#'))
			{
				color = color.Substring(1);
			}
			if (color.Length != 6)
			{
				return false;
			}

			for (var j = 0; j < color.Length; j++)
			{
				if (!ValidEx.Contains(color[j]))
				{
					return false;
				}
			}

			return true;
		}
	}
}
