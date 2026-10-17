namespace Greg.Xrm.Command.Services.Translations
{
	public sealed class TranslationParser : ITranslationParser
	{
		public IReadOnlyDictionary<int, string> Parse(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw Invalid("Translations are required.");

			var result = new Dictionary<int, string>();
			var position = 0;

			while (true)
			{
				SkipWhitespace(value, ref position);
				var lcid = ParseLcid(value, ref position);

				SkipWhitespace(value, ref position);
				Expect(value, ref position, ':');
				SkipWhitespace(value, ref position);

				var label = ParseLabel(value, ref position);
				if (result.ContainsKey(lcid))
					throw Invalid($"LCID {lcid} was specified more than once.");

				result.Add(lcid, label);

				SkipWhitespace(value, ref position);
				if (position == value.Length)
					return result;

				Expect(value, ref position, ',');
				SkipWhitespace(value, ref position);
				if (position == value.Length)
					throw Invalid("A translation is required after the last comma.");
			}
		}

		private static int ParseLcid(string value, ref int position)
		{
			var start = position;
			while (position < value.Length && char.IsDigit(value[position]))
				position++;

			if (start == position || !int.TryParse(value[start..position], out var lcid) || lcid <= 0)
				throw Invalid($"Expected a positive numeric LCID at position {start}.");

			return lcid;
		}

		private static string ParseLabel(string value, ref int position)
		{
			if (position >= value.Length || value[position] != '"')
				throw Invalid($"Expected a quoted label at position {position}.");

			position++;
			var label = new System.Text.StringBuilder();
			var closed = false;

			while (position < value.Length)
			{
				var character = value[position++];
				if (character == '"')
				{
					closed = true;
					break;
				}

				if (character == '\\')
				{
					if (position >= value.Length)
						throw Invalid("The label contains an incomplete escape sequence.");

					var escaped = value[position++];
					if (escaped is not ('"' or '\\'))
						throw Invalid($"Unsupported escape sequence '\\{escaped}'.");

					label.Append(escaped);
					continue;
				}

				label.Append(character);
			}

			if (!closed)
				throw Invalid("The label is missing its closing quote.");

			if (string.IsNullOrWhiteSpace(label.ToString()))
				throw Invalid("Translation labels cannot be empty.");

			return label.ToString();
		}

		private static void Expect(string value, ref int position, char expected)
		{
			if (position >= value.Length || value[position] != expected)
				throw Invalid($"Expected '{expected}' at position {position}.");

			position++;
		}

		private static void SkipWhitespace(string value, ref int position)
		{
			while (position < value.Length && char.IsWhiteSpace(value[position]))
				position++;
		}

		private static CommandException Invalid(string message)
		{
			return new CommandException(CommandException.CommandInvalidArgumentValue, message);
		}
	}
}
