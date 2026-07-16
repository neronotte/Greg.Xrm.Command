using System.Text.RegularExpressions;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public class QueryExecutorOData(string odataQuery) : IQueryExecutor
	{
		private const string FormattedValueSuffix = "@OData.Community.Display.V1.FormattedValue";
		private static readonly Regex EntityReferencePattern = new(@"^_(.+)_value$", RegexOptions.Compiled);

		public async Task<IReadOnlyCollection<Entity>> ExecuteQueryAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken)
		{
			var serviceClient = crm as ServiceClient
				?? throw new InvalidOperationException("The provided IOrganizationServiceAsync2 instance is not a ServiceClient.");

			var allEntities = new List<Entity>();
			var headers = new Dictionary<string, List<string>>
			{
				["Prefer"] = ["odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\""]
			};
			string? currentQuery = odataQuery;

			while (currentQuery != null)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var response = await serviceClient.ExecuteWebRequestAsync(HttpMethod.Get, currentQuery, null, headers, cancellationToken: cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					throw new InvalidOperationException($"OData query failed with status code {response.StatusCode}: {response.ReasonPhrase}");
				}

				var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
				var obj = JObject.Parse(responseContent);

				if (obj["value"] is JArray recordList && recordList.Count > 0)
				{
					allEntities.AddRange(ParseEntities(recordList));
				}

				var nextLink = obj["@odata.nextLink"]?.ToString();
				if (!string.IsNullOrEmpty(nextLink))
				{
					currentQuery = Uri.TryCreate(nextLink, UriKind.Absolute, out var nextUri)
						? Regex.Replace(nextUri.PathAndQuery, @"^/api/data/v[^/]+/", string.Empty, RegexOptions.IgnoreCase).TrimStart('/')
						: nextLink.TrimStart('/');
				}
				else
				{
					currentQuery = null;
				}
			}

			return allEntities;
		}


		private static List<Entity> ParseEntities(JArray recordList)
		{
			var entities = new List<Entity>();

			foreach (var record in recordList.OfType<JObject>())
			{
				var entity = ParseEntity(record);
				entities.Add(entity);
			}

			return entities;
		}


		private static Entity ParseEntity(JObject record)
		{
			var entity = new Entity();

			// First pass: collect all FormattedValue suffixes to know which attributes have formatted values
			var formattedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var property in record.Properties())
			{
				if (property.Name.EndsWith(FormattedValueSuffix, StringComparison.OrdinalIgnoreCase))
				{
					var attributeName = property.Name[..^FormattedValueSuffix.Length];
					formattedValues[attributeName] = property.Value?.ToString() ?? string.Empty;
				}
			}

			// Second pass: process actual attributes
			foreach (var property in record.Properties())
			{
				var propertyName = property.Name;

				// Skip OData metadata properties and formatted values (already processed)
if (propertyName.Contains('@'))
				{
					continue;
				}

				// Check if this is an EntityReference pattern: _something_value
				var entityRefMatch = EntityReferencePattern.Match(propertyName);
				if (entityRefMatch.Success)
				{
					var lookupAttributeName = entityRefMatch.Groups[1].Value;
					var attributeValue = ParseEntityReference(property.Value);
					if (attributeValue != null)
					{
						entity.Attributes[lookupAttributeName] = attributeValue;

						if (formattedValues.TryGetValue(propertyName, out var formattedValue))
						{
							entity.FormattedValues[lookupAttributeName] = formattedValue;
						}
					}
				}
				else
					{
						// Check if this is an OptionSetValue (integer with a formatted value)
						var hasFormattedValue = formattedValues.TryGetValue(propertyName, out var formattedValue);
						var attributeValue = ParseAttributeValue(property.Value, hasFormattedValue);
						entity.Attributes[propertyName] = attributeValue;

						if (hasFormattedValue)
						{
							entity.FormattedValues[propertyName] = formattedValue!;
						}
					}
			}

			return entity;
		}


		private static EntityReference? ParseEntityReference(JToken? value)
		{
			if (value == null || value.Type == JTokenType.Null)
			{
				return null;
			}

			var guidString = value.ToString();
			if (Guid.TryParse(guidString, out var guid))
			{
				return new EntityReference(string.Empty, guid);
			}

			return null;
		}


		private static object? ParseAttributeValue(JToken? value, bool hasFormattedValue)
		{
			if (value == null || value.Type == JTokenType.Null)
			{
				return null;
			}

			// If it's an integer with a formatted value, it's an OptionSetValue
			if (value.Type == JTokenType.Integer && hasFormattedValue)
			{
				return new OptionSetValue(value.Value<int>());
			}

			return value.Type switch
			{
				JTokenType.Integer => value.Value<long>(),
				JTokenType.Float => value.Value<decimal>(),
				JTokenType.Boolean => value.Value<bool>(),
				JTokenType.Date => value.Value<DateTime>(),
				JTokenType.Guid => value.Value<Guid>(),
				JTokenType.String => ParseStringValue(value.Value<string>()),
				_ => value.ToString()
			};
		}


		private static object? ParseStringValue(string? value)
		{
			if (value == null) return null;

			// Try to parse as Guid
			if (Guid.TryParse(value, out var guid))
			{
				return guid;
			}

			// Try to parse as DateTime (ISO 8601 format)
			if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateTime))
			{
				return dateTime;
			}

			return value;
		}
	}
}
