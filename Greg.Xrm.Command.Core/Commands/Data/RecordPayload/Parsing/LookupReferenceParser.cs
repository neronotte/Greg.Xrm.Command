using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing
{
	/// <summary>
	/// Parses lookup reference strings in the format entity(GUID) or entity(field='value')
	/// and resolves them to EntityReference objects.
	/// </summary>
	public static class LookupReferenceParser
	{
		// Matches: entityname(guid)
		private static readonly Regex GuidPattern = new(
			@"^([a-z][a-z0-9_]*)\(([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\)$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Matches: entityname(anything)
		private static readonly Regex FieldBasedPattern = new(
			@"^([a-z][a-z0-9_]*)\((.+)\)$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Matches: fieldname='value' (content inside parentheses)
		private static readonly Regex FieldValuePattern = new(
			@"^([a-z][a-z0-9_]+)='(.*)'$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

		/// <summary>
		/// Parses a lookup reference string and resolves it to an EntityReference.
		/// </summary>
		/// <param name="rawValue">The raw string value, e.g. systemuser(guid) or account(name='Acme').</param>
		/// <param name="fieldName">The logical name of the lookup field (for error messages).</param>
		/// <param name="crm">The CRM service used for field-based lookups.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>The resolved EntityReference.</returns>
		public static async Task<EntityReference> ParseAsync(
			string rawValue,
			string fieldName,
			IOrganizationServiceAsync2 crm,
			CancellationToken cancellationToken)
		{
			// Try GUID pattern first
			var guidMatch = GuidPattern.Match(rawValue);
			if (guidMatch.Success)
			{
				var entityName = guidMatch.Groups[1].Value.ToLowerInvariant();
				var guid = Guid.Parse(guidMatch.Groups[2].Value);
				if (guid == Guid.Empty)
				{
					throw new FormatException(
						$"Invalid lookup reference format for field '{fieldName}': '{rawValue}'. " +
						$"Expected formats: entity(GUID) or entity(fieldname='value').");
				}

				return new EntityReference(entityName, guid);
			}

			// Try field-based pattern
			var fieldBasedMatch = FieldBasedPattern.Match(rawValue);
			if (fieldBasedMatch.Success)
			{
				var entityName = fieldBasedMatch.Groups[1].Value.ToLowerInvariant();
				var content = fieldBasedMatch.Groups[2].Value;

				var fieldValueMatch = FieldValuePattern.Match(content);
				if (!fieldValueMatch.Success)
				{
					throw new FormatException(
						$"Invalid lookup reference format for field '{fieldName}': '{rawValue}'. " +
						$"Expected formats: entity(GUID) or entity(fieldname='value').");
				}

				var lookupFieldName = fieldValueMatch.Groups[1].Value;
				var rawFieldValue = fieldValueMatch.Groups[2].Value;
				// Unescape '' -> '
				var fieldValue = rawFieldValue.Replace("''", "'");

				// Retrieve entity metadata to get EntitySetName and PrimaryIdAttribute
				EntityMetadata entityMetadata;
				try
				{
					var metadataRequest = new RetrieveEntityRequest
					{
						LogicalName = entityName,
						EntityFilters = EntityFilters.Entity
					};
					var metadataResponse = (RetrieveEntityResponse)await crm.ExecuteAsync(metadataRequest, cancellationToken);
					entityMetadata = metadataResponse.EntityMetadata;
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					throw new InvalidOperationException(
						$"Failed to retrieve metadata for entity '{entityName}' while resolving field '{fieldName}': {ex.Message}", ex);
				}

				var primaryKey = entityMetadata.PrimaryIdAttribute;


				// Query for matching records
				var query = new QueryExpression(entityName)
				{
					ColumnSet = new ColumnSet(primaryKey),
					NoLock = true,
					TopCount = 2
				};
				query.Criteria.AddCondition(lookupFieldName, ConditionOperator.Equal, fieldValue);

				EntityCollection queryResult;
				try
				{
					queryResult = await crm.RetrieveMultipleAsync(query, cancellationToken);
				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					throw new InvalidOperationException(
						$"Error querying '{entityName}' by {lookupFieldName}='{fieldValue}' for field '{fieldName}': {ex.Message}", ex);
				}

				if (queryResult.Entities.Count == 0)
				{
					throw new InvalidOperationException(
						$"No {entityName} record found with {lookupFieldName} = '{fieldValue}'.");
				}

				if (queryResult.Entities.Count >= 2)
				{
					throw new InvalidOperationException(
						$"Ambiguous lookup: at least 2 {entityName} records match {lookupFieldName} = '{fieldValue}'. Use a GUID instead.");
				}

				var resolvedId = queryResult.Entities[0].Id;
				return new EntityReference(entityName, resolvedId);
			}

			// No pattern matched
			throw new FormatException(
				$"Invalid lookup reference format for field '{fieldName}': '{rawValue}'. " +
				$"Expected formats: entity(GUID) or entity(fieldname='value').");
		}
	}
}
