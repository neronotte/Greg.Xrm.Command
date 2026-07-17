using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	public class StringFieldValueConverter : IFieldValueConverter
	{
		public object? Convert(object? rawValue, AttributeMetadata metadata, string fieldName)
		{
			if (rawValue == null)
				return null;

			if (rawValue is string str)
				return string.IsNullOrEmpty(str) ? null : str;

			return rawValue.ToString();
		}
	}
}
