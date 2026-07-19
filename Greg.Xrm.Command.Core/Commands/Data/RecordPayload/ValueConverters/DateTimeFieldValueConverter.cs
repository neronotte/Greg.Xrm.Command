using System.Globalization;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class DateTimeFieldValueConverter : IFieldValueConverter
	{
		public Task<object?> ConvertAsync(object? rawValue, AttributeMetadata metadata, string fieldName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (rawValue == null)
				return Task.FromResult<object?>(null);

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
					return Task.FromResult<object?>(new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Unspecified));
				}

				throw new FormatException(
					$"Cannot convert '{strValue}' to DateOnly for field '{fieldName}'. Expected format: yyyy-MM-dd.");
			}

			var behavior = (metadata as DateTimeAttributeMetadata)?.DateTimeBehavior?.Value;

			var offsetFormats = new[]
			{
				"yyyy-MM-dd'T'HH:mm:ssK",
				"yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
			};

			if (DateTimeOffset.TryParseExact(
					strValue,
					offsetFormats,
					CultureInfo.InvariantCulture,
					DateTimeStyles.None,
					out var offsetDateTime))
			{
				if (string.Equals(behavior, DateTimeBehavior.TimeZoneIndependent.Value, StringComparison.OrdinalIgnoreCase))
					return Task.FromResult<object?>(offsetDateTime.DateTime);

				return Task.FromResult<object?>(offsetDateTime.UtcDateTime);
			}

			var isoFormats = new[]
			{
				"yyyy-MM-dd'T'HH:mm:ss",
				"yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
				"yyyy-MM-dd",
			};

			if (DateTime.TryParseExact(
					strValue,
					isoFormats,
					CultureInfo.InvariantCulture,
					DateTimeStyles.None,
					out var dateTime))
			{
				return Task.FromResult<object?>(dateTime);
			}

			throw new FormatException(
				$"Cannot convert '{strValue}' to DateTime for field '{fieldName}'. Expected ISO 8601 format, e.g. 2024-01-15T08:30:00Z.");
		}
	}
}
