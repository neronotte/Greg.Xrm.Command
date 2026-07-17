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
				dtMeta.DateTimeBehavior == DateTimeBehavior.DateOnly)
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

			// DateAndTime or UserLocal - accept ISO 8601
			if (DateTime.TryParse(strValue, null, DateTimeStyles.RoundtripKind, out var dateTime))
			{
				return dateTime;
			}

			throw new FormatException(
				$"Cannot convert '{strValue}' to DateTime for field '{fieldName}'. Expected ISO 8601 format, e.g. 2024-01-15T08:30:00Z.");
		}
	}
}
