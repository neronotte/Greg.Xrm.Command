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
	/// resolve field-based lookups asynchronously.
	/// </remarks>
	public class LookupFieldValueConverter : IFieldValueConverter
	{
		private readonly IOrganizationServiceAsync2 _crm;

		public LookupFieldValueConverter(IOrganizationServiceAsync2 crm)
		{
			_crm = crm;
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

			var reference = await LookupReferenceParser.ParseAsync(strValue, fieldName, _crm, cancellationToken);
			if (metadata is LookupAttributeMetadata lookupMetadata &&
				lookupMetadata.Targets is { Length: > 0 } targets &&
				!targets.Contains(reference.LogicalName, StringComparer.OrdinalIgnoreCase))
			{
				throw new FormatException(
					$"Entity '{reference.LogicalName}' is not a valid target for lookup field '{fieldName}'. Valid targets: {string.Join(", ", targets)}.");
			}

			return reference;
		}
	}
}
