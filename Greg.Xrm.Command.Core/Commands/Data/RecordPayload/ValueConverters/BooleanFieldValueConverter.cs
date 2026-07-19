using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class BooleanFieldValueConverter : IFieldValueConverter
	{
		public Task<object?> ConvertAsync(object? rawValue, AttributeMetadata metadata, string fieldName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (rawValue == null)
				return Task.FromResult<object?>(null);

			if (rawValue is bool b)
				return Task.FromResult<object?>(b);

			if (rawValue is string s)
			{
				if (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1")
					return Task.FromResult<object?>(true);
				if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s == "0")
					return Task.FromResult<object?>(false);
				throw new FormatException(
					$"Cannot convert '{s}' to boolean for field '{fieldName}'. Accepted values: true, false, 1, 0.");
			}

			if (rawValue is long l)
			{
				if (l == 1) return Task.FromResult<object?>(true);
				if (l == 0) return Task.FromResult<object?>(false);
			}

			throw new FormatException(
				$"Cannot convert value '{rawValue}' of type '{rawValue.GetType().Name}' to boolean for field '{fieldName}'. Accepted values: true, false, 1, 0.");
		}
	}
}
