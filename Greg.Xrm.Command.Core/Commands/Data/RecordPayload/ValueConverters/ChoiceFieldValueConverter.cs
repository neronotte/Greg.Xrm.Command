using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class ChoiceFieldValueConverter : IFieldValueConverter
	{
		public Task<object?> ConvertAsync(object? rawValue, AttributeMetadata metadata, string fieldName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (rawValue == null)
				return Task.FromResult<object?>(null);

			if (rawValue is long l)
			{
				if (l < int.MinValue || l > int.MaxValue)
				{
					throw new FormatException(
						$"Cannot convert value '{l}' to choice value for field '{fieldName}': value is out of Int32 range.");
				}

				return Task.FromResult<object?>(new OptionSetValue(ResolveNumericCode((int)l, metadata, fieldName)));
			}

			if (rawValue is int i)
				return Task.FromResult<object?>(new OptionSetValue(ResolveNumericCode(i, metadata, fieldName)));

			if (rawValue is string s)
			{
				if (int.TryParse(s, out var numericCode))
					return Task.FromResult<object?>(new OptionSetValue(ResolveNumericCode(numericCode, metadata, fieldName)));

				var options = GetOptions(metadata);

				if (options != null)
				{
					var matches = options
						.Where(option => option.Value.HasValue && string.Equals(GetLabel(option), s, StringComparison.OrdinalIgnoreCase))
						.ToList();

					if (matches.Count == 1)
						return Task.FromResult<object?>(new OptionSetValue(matches[0].Value!.Value));

					if (matches.Count > 1)
						throw new FormatException(
							$"Choice label '{s}' is ambiguous for field '{fieldName}'. Use an integer code instead.");

					var validLabels = string.Join(", ", options
						.Select(GetLabel)
						.Where(l => l != null));

					throw new FormatException(
						$"Cannot convert '{s}' to choice value for field '{fieldName}'. " +
						$"Valid labels are: {validLabels}.");
				}
			}

			throw new FormatException(
				$"Cannot convert value '{rawValue}' to choice value for field '{fieldName}'.");
		}

		private static int ResolveNumericCode(int code, AttributeMetadata metadata, string fieldName)
		{
			var options = GetOptions(metadata);
			if (options == null || !options.Any(option => option.Value == code))
			{
				var validCodes = options == null
					? string.Empty
					: string.Join(", ", options
						.Where(option => option.Value.HasValue)
						.Select(option => option.Value!.Value.ToString()));

				var message = $"Cannot convert '{code}' to choice value for field '{fieldName}'.";
				if (!string.IsNullOrWhiteSpace(validCodes))
				{
					message += $" Valid codes are: {validCodes}.";
				}

				throw new FormatException(message);
			}

			return code;
		}

		private static OptionMetadataCollection? GetOptions(AttributeMetadata metadata) =>
			metadata switch
			{
				PicklistAttributeMetadata pl => pl.OptionSet?.Options,
				StateAttributeMetadata st => st.OptionSet?.Options,
				StatusAttributeMetadata su => su.OptionSet?.Options,
				MultiSelectPicklistAttributeMetadata ms => ms.OptionSet?.Options,
				_ => null
			};

		private static string? GetLabel(OptionMetadata option) =>
			option.Label?.UserLocalizedLabel?.Label
			?? option.Label?.LocalizedLabels?.Cast<LocalizedLabel>().FirstOrDefault()?.Label;
	}
}
