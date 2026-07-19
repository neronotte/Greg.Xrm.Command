using System.Text.Json;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing
{
	/// <summary>
	/// Parses a JSON string or file into a dictionary of field names to raw .NET values.
	/// </summary>
	public static class JsonPayloadParser
	{
		/// <summary>
		/// Parses a JSON string into a dictionary of field names to raw .NET values.
		/// </summary>
		/// <param name="json">The JSON string to parse.</param>
		/// <returns>A dictionary mapping field names to their .NET values.</returns>
		/// <exception cref="JsonException">Thrown if the JSON is malformed.</exception>
		/// <exception cref="InvalidOperationException">Thrown if any field value is a JSON object.</exception>
		public static Dictionary<string, object?> ParseInline(string json)
		{
			using var document = JsonDocument.Parse(json);
			var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

			foreach (var property in document.RootElement.EnumerateObject())
			{
				result[property.Name] = ConvertElement(property.Value);
			}

			return result;
		}

		/// <summary>
		/// Parses a JSON file into a dictionary of field names to raw .NET values.
		/// </summary>
		/// <param name="path">The path to the JSON file.</param>
		/// <returns>A dictionary mapping field names to their .NET values.</returns>
		public static Dictionary<string, object?> ParseFile(string path)
		{
			var json = File.ReadAllText(path);
			return ParseInline(json);
		}

		private static object? ConvertElement(JsonElement element)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.String:
					return element.GetString();

case JsonValueKind.Number:
	if (element.TryGetInt64(out var longValue))
		return longValue;
	if (element.TryGetDecimal(out var decimalValue))
		return decimalValue;
	return element.GetDouble();

				case JsonValueKind.True:
					return true;

				case JsonValueKind.False:
					return false;

				case JsonValueKind.Null:
					return null;

				case JsonValueKind.Array:
					var list = new List<object?>();
					foreach (var item in element.EnumerateArray())
					{
						list.Add(ConvertElement(item));
					}
					return list;

				case JsonValueKind.Object:
					throw new InvalidOperationException(
						"JSON objects are not supported as field values. Use the lookup reference syntax: entity(GUID) or entity(field='value').");

				default:
					return null;
			}
		}
	}
}
