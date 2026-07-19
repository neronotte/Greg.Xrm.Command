using System.Globalization;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class DateTimeFieldValueConverter : IFieldValueConverter
	{
		public object? Convert(object? rawValue, AttributeMetadata metadata, string fieldName)
		{
			if (rawValue == null)
				return null;

			var strValue = rawValue as string ?? rawValue.ToString()!;

			if (metadata is DateTimeAttributeMetadata dtMeta &&
				string.Equals(dtMeta.DateTimeBehavior?.Value, DateTimeBehavior.DateOnly.Value, StringComparison.OrdinalIgnoreCase))
			{
				if (DateTime.TryParseExact(
						strValue,
						"yyyy-MM-dd",
						CultureInfo.InvariantCulture,
						DateTimeStyles.None,
						out var dateOnly))
				{
					return new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Utc);
				}

				throw new FormatException(
					$"Cannot convert '{strValue}' to DateOnly for field '{fieldName}'. Expected format: yyyy-MM-dd.");
			}

			// DateAndTime or UserLocal - accept ISO 8601 international format only
			var isoFormats = new[]
			{
				"yyyy-MM-ddTHH:mm:ssZ",
				"yyyy-MM-ddTHH:mm:sszzz",
				"yyyy-MM-ddTHH:mm:ss",
				"yyyy-MM-ddTHH:mm:ss.fffZ",
				"yyyy-MM-ddTHH:mm:ss.fffzzz",
				"yyyy-MM-ddTHH:mm:ss.fff",
				"yyyy-MM-dd",
			};

			if (DateTime.TryParseExact(
					strValue,
					isoFormats,
					CultureInfo.InvariantCulture,
					DateTimeStyles.RoundtripKind,
					out var dateTime))
			{
				return dateTime;
			}

			throw new FormatException(
				$"Cannot convert '{strValue}' to DateTime for field '{fieldName}'. Expected ISO 8601 format, e.g. 2024-01-15T08:30:00Z.");
		}
	}
}
