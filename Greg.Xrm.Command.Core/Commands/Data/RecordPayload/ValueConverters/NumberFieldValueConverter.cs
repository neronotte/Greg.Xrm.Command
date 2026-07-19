using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class NumberFieldValueConverter : IFieldValueConverter
	{
		public object? Convert(object? rawValue, AttributeMetadata metadata, string fieldName)
		{
			if (rawValue == null)
				return null;

			return metadata switch
			{
				IntegerAttributeMetadata => ConvertToInt(rawValue, fieldName),
				DecimalAttributeMetadata => ConvertToDecimal(rawValue, fieldName),
				DoubleAttributeMetadata => ConvertToDouble(rawValue, fieldName),
				MoneyAttributeMetadata => new Money(ConvertToDecimal(rawValue, fieldName)),
				_ => throw new InvalidOperationException($"Unsupported numeric metadata type '{metadata.GetType().Name}' for field '{fieldName}'.")
			};
		}

		private static int ConvertToInt(object rawValue, string fieldName)
		{
			return rawValue switch
			{
				long l => checked((int)l),
				int i => i,
				decimal dec when dec == decimal.Truncate(dec) && dec >= int.MinValue && dec <= int.MaxValue => (int)dec,
				double d when double.IsFinite(d) && d == Math.Truncate(d) && d >= int.MinValue && d <= int.MaxValue => (int)d,
				string s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
					? result
					: throw new FormatException($"Cannot convert '{s}' to integer for field '{fieldName}'."),
				_ => throw new FormatException($"Cannot convert value of type '{rawValue.GetType().Name}' to integer for field '{fieldName}'.")
			};
		}

		private static decimal ConvertToDecimal(object rawValue, string fieldName)
		{
			return rawValue switch
			{
				long l => (decimal)l,
				int i => (decimal)i,
				double d => (decimal)d,
				decimal dec => dec,
				string s => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
					? result
					: throw new FormatException($"Cannot convert '{s}' to decimal for field '{fieldName}'."),
				_ => throw new FormatException($"Cannot convert value of type '{rawValue.GetType().Name}' to decimal for field '{fieldName}'.")
			};
		}

		private static double ConvertToDouble(object rawValue, string fieldName)
		{
			return rawValue switch
			{
				long l => (double)l,
				int i => (double)i,
				double d when double.IsFinite(d) => d,
				decimal dec => (double)dec,
				string s when double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
					&& double.IsFinite(result) => result,
				string s => throw new FormatException($"Cannot convert '{s}' to double for field '{fieldName}'."),
				_ => throw new FormatException($"Cannot convert value of type '{rawValue.GetType().Name}' to double for field '{fieldName}'.")
			};
		}
	}
}
