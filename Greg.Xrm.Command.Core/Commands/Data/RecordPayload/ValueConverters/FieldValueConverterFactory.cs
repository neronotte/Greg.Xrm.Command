using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	/// <summary>
	/// Factory that maps AttributeMetadata types to the appropriate IFieldValueConverter.
	/// </summary>
	public static class FieldValueConverterFactory
	{
		/// <summary>
		/// Returns the appropriate converter for the given attribute metadata.
		/// </summary>
		/// <param name="metadata">The attribute metadata for the target field.</param>
		/// <param name="crm">CRM service, required for lookup fields.</param>
		/// <returns>
		/// The converter, or <c>null</c> if the field type is not supported
		/// (e.g. File or Image — the caller should emit a warning and skip the field).
		/// </returns>
		public static IFieldValueConverter? GetConverter(AttributeMetadata metadata, IOrganizationServiceAsync2? crm = null)
		{
			return metadata switch
			{
				StringAttributeMetadata or MemoAttributeMetadata => new StringFieldValueConverter(),

				IntegerAttributeMetadata
				or DecimalAttributeMetadata
				or DoubleAttributeMetadata
				or MoneyAttributeMetadata => new NumberFieldValueConverter(),

				BooleanAttributeMetadata => new BooleanFieldValueConverter(),

				DateTimeAttributeMetadata => new DateTimeFieldValueConverter(),

				PicklistAttributeMetadata
				or StateAttributeMetadata
				or StatusAttributeMetadata => new ChoiceFieldValueConverter(),

				MultiSelectPicklistAttributeMetadata => new MultiSelectChoiceFieldValueConverter(),

				// LookupAttributeMetadata covers Lookup, Customer and Owner attribute types
				LookupAttributeMetadata => crm != null
					? new LookupFieldValueConverter(crm)
					: throw new InvalidOperationException(
						"A CRM service instance is required to resolve lookup fields."),

				FileAttributeMetadata or ImageAttributeMetadata => null,

				_ => null
			};
		}
	}
}
