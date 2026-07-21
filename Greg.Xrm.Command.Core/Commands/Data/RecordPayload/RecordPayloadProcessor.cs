using Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload
{
	/// <summary>
	/// Processes a raw payload dictionary into a typed Entity, validating field names and
	/// converting values using the appropriate <see cref="IFieldValueConverter"/>.
	/// </summary>
	public class RecordPayloadProcessor
	{
		private enum ValidationMode
		{
			Create,
			Update,
			Upsert
		}

		public record ProcessResult(
			Entity Entity,
			IReadOnlyList<string> Warnings,
			IReadOnlyList<string> Errors);

		/// <summary>
		/// Processes a raw payload dictionary against the entity metadata.
		/// All errors are accumulated before returning.
		/// </summary>
		/// <param name="rawPayload">Dictionary of field name → raw value from the input.</param>
		/// <param name="entityMetadata">Metadata for the target entity (must include Attributes).</param>
		/// <param name="validatingForCreate">
		/// True when creating a record (checks IsValidForCreate);
		/// false when updating a record (checks IsValidForUpdate).
		/// </param>
		/// <param name="crm">CRM service used for lookup resolution.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>A <see cref="ProcessResult"/> containing the entity, warnings, and errors.</returns>
		public async Task<ProcessResult> ProcessAsync(
			Dictionary<string, object?> rawPayload,
			EntityMetadata entityMetadata,
			bool validatingForCreate,
			IOrganizationServiceAsync2 crm,
			CancellationToken cancellationToken)
		{
			return await ProcessAsync(
				rawPayload,
				entityMetadata,
				validatingForCreate ? ValidationMode.Create : ValidationMode.Update,
				crm,
				cancellationToken);
		}

		public async Task<ProcessResult> ProcessForUpsertAsync(
			Dictionary<string, object?> rawPayload,
			EntityMetadata entityMetadata,
			IOrganizationServiceAsync2 crm,
			CancellationToken cancellationToken)
		{
			return await ProcessAsync(
				rawPayload,
				entityMetadata,
				ValidationMode.Upsert,
				crm,
				cancellationToken);
		}

		private async Task<ProcessResult> ProcessAsync(
			Dictionary<string, object?> rawPayload,
			EntityMetadata entityMetadata,
			ValidationMode validationMode,
			IOrganizationServiceAsync2 crm,
			CancellationToken cancellationToken)
		{
			var entity = new Entity(entityMetadata.LogicalName);
			var warnings = new List<string>();
			var errors = new List<string>();

			// Build a lookup by logical name for O(1) access
			var attributeIndex = entityMetadata.Attributes
				.ToDictionary(a => a.LogicalName, a => a, StringComparer.OrdinalIgnoreCase);

			foreach (var (fieldName, rawValue) in rawPayload)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// 1. Find attribute metadata
				if (!attributeIndex.TryGetValue(fieldName, out var attrMeta))
				{
					errors.Add($"Field '{fieldName}' was not found in the '{entityMetadata.LogicalName}' table metadata.");
					continue;
				}

				// 2. Skip File and Image fields with warning
				if (attrMeta is FileAttributeMetadata || attrMeta is ImageAttributeMetadata)
				{
					warnings.Add($"Field '{fieldName}' is a File or Image field and cannot be set via the SDK. It will be skipped.");
					continue;
				}

				// 3. Check IsValidForCreate / IsValidForUpdate
				if (validationMode == ValidationMode.Create && attrMeta.IsValidForCreate == false)
				{
					warnings.Add($"Field '{fieldName}' is not valid for create and will be skipped.");
					continue;
				}
				if (validationMode == ValidationMode.Update && attrMeta.IsValidForUpdate == false)
				{
					warnings.Add($"Field '{fieldName}' is not valid for update and will be skipped.");
					continue;
				}

				// 4. Null / empty → set to null
				if (rawValue == null || (rawValue is string sv && sv.Length == 0))
				{
					entity[fieldName] = null;
					continue;
				}

				// 5. Convert the value
				try
				{
					var converter = FieldValueConverterFactory.GetConverter(attrMeta, crm);
					if (converter == null)
					{
						warnings.Add($"Field '{fieldName}' has an unsupported type '{attrMeta.AttributeType}' and will be skipped.");
						continue;
					}

					var converted = await converter.ConvertAsync(rawValue, attrMeta, fieldName, cancellationToken);
					if (converted is SkippedFieldValue)
					{
						warnings.Add($"Field '{fieldName}' cannot be set and will be skipped.");
						continue;
					}

					entity[fieldName] = converted;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					errors.Add($"Error converting field '{fieldName}': {ex.Message}");
				}
			}

			return new ProcessResult(entity, warnings, errors);
		}
	}
}
