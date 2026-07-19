using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class MultiSelectChoiceFieldValueConverter : IFieldValueConverter
	{
		private readonly ChoiceFieldValueConverter _singleConverter = new();

		public async Task<object?> ConvertAsync(object? rawValue, AttributeMetadata metadata, string fieldName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (rawValue == null)
				return null;

			var items = new List<OptionSetValue>();

			if (rawValue is List<object?> list)
			{
				foreach (var item in list)
				{
					var converted = await _singleConverter.ConvertAsync(item, metadata, fieldName, cancellationToken);
					if (converted is not OptionSetValue osv)
					{
						throw new FormatException(
							$"Cannot convert null item to multi-select choice for field '{fieldName}'.");
					}
					items.Add(osv);
				}
			}
			else if (rawValue is string s)
			{
				var tokens = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var token in tokens)
				{
					var converted = await _singleConverter.ConvertAsync(token, metadata, fieldName, cancellationToken);
					if (converted is OptionSetValue osv)
						items.Add(osv);
				}
			}
			else
			{
				throw new FormatException(
					$"Cannot convert value '{rawValue}' to multi-select choice for field '{fieldName}'. " +
					$"Provide a comma-separated string or a JSON array of integers/strings.");
			}

			return new OptionSetValueCollection(items);
		}
	}
}
