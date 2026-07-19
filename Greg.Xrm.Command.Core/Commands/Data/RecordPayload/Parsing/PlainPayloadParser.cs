namespace Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing
{
	/// <summary>
	/// Parses a semicolon-separated list of field=value pairs from the --plain option.
	/// </summary>
	/// <remarks>
	/// Parsing rules:
	/// - Pairs are separated by semicolons (;)
	/// - The first '=' separates key from value; subsequent '=' chars are part of the value
	/// - When inside parentheses (depth > 0), single quotes are treated as literal characters
	///   and semicolons do not terminate the current pair
	/// - At the top level (depth = 0), single quotes delimit sections where ';' is not a
	///   separator. The quotes themselves are NOT included in the output.
	/// - Two consecutive single quotes ('') at any nesting level represent a literal single quote
	/// - An empty value (field=) is valid and represents null/empty
	/// </remarks>
	public static class PlainPayloadParser
	{
		private enum State
		{
			ReadingKey,
			ReadingValue,
			InsideTopLevelQuotes
		}

		public static Dictionary<string, string> Parse(string input)
		{
			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			if (string.IsNullOrEmpty(input))
				return result;

			var state = State.ReadingKey;
			var key = new System.Text.StringBuilder();
			var value = new System.Text.StringBuilder();
			var parenDepth = 0;
			var insideParenthesizedQuotes = false;
			var i = 0;

			while (i < input.Length)
			{
				var ch = input[i];

				switch (state)
				{
					case State.ReadingKey:
						if (ch == '=')
						{
							state = State.ReadingValue;
							i++;
						}
						else
						{
							key.Append(ch);
							i++;
						}
						break;

					case State.ReadingValue:
						if (ch == '\'' && parenDepth > 0)
						{
							if (i + 1 < input.Length && input[i + 1] == '\'')
							{
								value.Append('\'');
								i += 2;
							}
							else
							{
								insideParenthesizedQuotes = !insideParenthesizedQuotes;
								value.Append(ch);
								i++;
							}
						}
						else if (ch == '(' && !insideParenthesizedQuotes)
						{
							if (parenDepth > 0 || IsLookupReferencePrefix(value))
								parenDepth++;
							value.Append(ch);
							i++;
						}
						else if (ch == ')' && !insideParenthesizedQuotes)
						{
							if (parenDepth > 0) parenDepth--;
							value.Append(ch);
							i++;
						}
						else if (ch == ';' && parenDepth == 0)
						{
							// End of this pair (only when not inside parentheses)
							EmitPair(result, key, value);
							key.Clear();
							value.Clear();
							state = State.ReadingKey;
							i++;
						}
						else if (ch == '\'' && parenDepth == 0)
						{
							// At top level: check for escaped quote ''
							if (i + 1 < input.Length && input[i + 1] == '\'')
							{
								// Two single quotes → literal single quote in output
								value.Append('\'');
								i += 2;
							}
							else
							{
								// Opening quote at top level: enter quoted section, do NOT include quote in output
								state = State.InsideTopLevelQuotes;
								i++;
							}
						}
						else
						{
							// Inside parentheses: single quotes and semicolons are literal
							value.Append(ch);
							i++;
						}
						break;

					case State.InsideTopLevelQuotes:
						if (ch == '\'')
						{
							// Check for escaped quote ''
							if (i + 1 < input.Length && input[i + 1] == '\'')
							{
								// Two single quotes inside quoted section → literal single quote
								value.Append('\'');
								i += 2;
							}
							else
							{
								// Closing quote: exit quoted section, do NOT include quote in output
								state = State.ReadingValue;
								i++;
							}
						}
						else
						{
							value.Append(ch);
							i++;
						}
						break;
				}
			}

			if (state == State.InsideTopLevelQuotes || parenDepth != 0)
			{
				throw new FormatException("Malformed payload: unclosed quote or parenthesis.");
			}

			if (key.Length > 0 || state == State.ReadingValue)
			{
				if (state == State.ReadingKey)
					throw new FormatException($"Malformed payload token '{key}': expected field=value.");

				EmitPair(result, key, value);
			}

			return result;
		}

		private static bool IsLookupReferencePrefix(System.Text.StringBuilder value)
		{
			if (value.Length == 0)
				return false;

			for (var index = 0; index < value.Length; index++)
			{
				var ch = value[index];
				if (!(char.IsLetterOrDigit(ch) || ch == '_'))
					return false;
			}

			return true;
		}

		private static void EmitPair(Dictionary<string, string> result, System.Text.StringBuilder key, System.Text.StringBuilder value)
		{
			var fieldName = key.ToString().Trim();
			if (string.IsNullOrEmpty(fieldName))
				throw new FormatException("Malformed payload: field name cannot be empty.");

			result[fieldName] = value.ToString();
		}
	}
}
