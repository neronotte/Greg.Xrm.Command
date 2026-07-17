using Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	/// <summary>
	/// Converter for lookup fields. Delegates to <see cref="LookupReferenceParser"/>
	/// to resolve entity(GUID) or entity(field='value') references.
	/// </summary>
	/// <remarks>
	/// This converter is special: it requires an <see cref="IOrganizationServiceAsync2"/> to
	/// resolve field-based lookups asynchronously. Callers must use
	/// <see cref="ConvertAsync"/> instead of the synchronous interface method.
	/// </remarks>
	public class LookupFieldValueConverter : IFieldValueConverter
	{
		private readonly IOrganizationServiceAsync2 _crm;

		public LookupFieldValueConverter(IOrganizationServiceAsync2 crm)
		{
			_crm = crm;
		}

		/// <summary>
		/// Not supported — use <see cref="ConvertAsync"/> for lookup fields.
		/// </summary>
		public object? Convert(object? rawValue, AttributeMetadata metadata, string fieldName)
		{
			// Synchronous path: throw so callers use ConvertAsync
			return ConvertAsync(rawValue, metadata, fieldName, CancellationToken.None).GetAwaiter().GetResult();
		}

		public async Task<object?> ConvertAsync(
			object? rawValue,
			AttributeMetadata metadata,
			string fieldName,
			CancellationToken cancellationToken)
		{
			if (rawValue == null)
				return null;

			var strValue = rawValue as string
				?? throw new FormatException(
					$"Expected a string reference for lookup field '{fieldName}', but got '{rawValue.GetType().Name}'.");

			return await LookupReferenceParser.ParseAsync(strValue, fieldName, _crm, cancellationToken);
		}
	}
}
