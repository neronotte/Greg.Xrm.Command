using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class StringFieldValueConverter : IFieldValueConverter
	{
		public Task<object?> ConvertAsync(object? rawValue, AttributeMetadata metadata, string fieldName, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (rawValue == null)
				return Task.FromResult<object?>(null);

			if (rawValue is string str)
				return Task.FromResult<object?>(string.IsNullOrEmpty(str) ? null : str);

			return Task.FromResult<object?>(System.Convert.ToString(rawValue, System.Globalization.CultureInfo.InvariantCulture));
		}
	}
}
