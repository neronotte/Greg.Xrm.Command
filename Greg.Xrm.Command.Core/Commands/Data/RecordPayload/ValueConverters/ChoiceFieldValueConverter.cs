using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class ChoiceFieldValueConverter : IFieldValueConverter
	{
		public object? Convert(object? rawValue, AttributeMetadata metadata, string fieldName)
		{
			if (rawValue == null)
				return null;

			// If it's a long (from JSON number), convert directly
			if (rawValue is long l)
				return new OptionSetValue((int)l);

			// If it's an int
			if (rawValue is int i)
				return new OptionSetValue(i);

			if (rawValue is string s)
			{
				// Try numeric parse first
				if (int.TryParse(s, out var numericCode))
					return new OptionSetValue(numericCode);

				// Try label match
				OptionMetadataCollection? options = metadata switch
				{
					PicklistAttributeMetadata pl => pl.OptionSet?.Options,
					StateAttributeMetadata st => st.OptionSet?.Options,
					StatusAttributeMetadata su => su.OptionSet?.Options,
					MultiSelectPicklistAttributeMetadata ms => ms.OptionSet?.Options,
					_ => null
				};

				if (options != null)
				{
					foreach (var option in options)
					{
						var label = GetLabel(option);
						if (label != null && label.Equals(s, StringComparison.OrdinalIgnoreCase))
						{
							return new OptionSetValue(option.Value!.Value);
						}
					}

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

		private static string? GetLabel(OptionMetadata option) =>
			option.Label?.UserLocalizedLabel?.Label
			?? option.Label?.LocalizedLabels?.Cast<LocalizedLabel>().FirstOrDefault()?.Label;
	}
}
