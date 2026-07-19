using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	/// <summary>
	/// Converts a raw value (from plain or JSON input) to the appropriate Dataverse SDK type
	/// based on the target field's AttributeMetadata.
	/// </summary>
	public interface IFieldValueConverter
	{
		/// <summary>
		/// Converts the raw value to the appropriate SDK type.
		/// </summary>
		/// <param name="rawValue">The raw value from the input payload.</param>
		/// <param name="metadata">The attribute metadata for the target field.</param>
		/// <param name="fieldName">The logical name of the field (for error messages).</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The converted value ready to assign to an Entity attribute.</returns>
		Task<object?> ConvertAsync(object? rawValue, AttributeMetadata metadata, string fieldName, CancellationToken cancellationToken);
	}
}
