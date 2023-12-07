using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using System.Text;

namespace Greg.Xrm.Command
{
	public static class Extensions
	{


		public static string SplitNameInPartsByCapitalLetters(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;

			var sb = new StringBuilder();
			char previousChar = 'a';
			for (int i = 0; i < text.Length; i++)
			{
				var c = text[i];
				var nextChar = i + 1 < text.Length ? text[i + 1] : 'a';
				if (char.IsUpper(c)		
					&& sb.Length > 0 
					&& (!char.IsUpper(previousChar) || (char.IsUpper(previousChar) && !char.IsUpper(nextChar)))) sb.Append(' ');
				sb.Append(c);
				previousChar = c;
			}
			return sb.ToString();
		}

		public static string OnlyLettersNumbersOrUnderscore(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;

			var sb = new StringBuilder();
			foreach (var c in text)
			{
				if (char.IsLetterOrDigit(c) || c == '_')
				{
					sb.Append(c.ToString().ToLowerInvariant());
				}
			}
			return sb.ToString();
		}

		public static string UpperCaseInitials(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			if (text.Length == 1) return text.ToUpperInvariant();

			var initials = text
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x[0].ToString().ToUpperInvariant())
				.ToArray();

			if (initials.Length == 1) return initials[0];

			return string.Join(string.Empty, initials);
		}

		public static string Left(this string? text, int len)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			if (text.Length <= len) return text;
			return text[..len];
		}

		public static bool IsOnlyLowercaseLettersOrNumbers(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text)) return false;

			foreach (var c in text)
			{
				if (!char.IsLetterOrDigit(c)) return false;
				if (char.IsLetter(c) && char.IsUpper(c)) return false;
			}
			return true;
		}


		public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				return value;
			}
			return default;
		}


		public static string? GetLocalizedLabel(this Label? label, int defaultLanguageCode)
		{
			if (label is null) return null;
			return label.LocalizedLabels.FirstOrDefault(x => x.LanguageCode == defaultLanguageCode)?.Label ?? label.UserLocalizedLabel?.Label;
		}


		/// <summary>
		/// Determines whether the entity can participate in a many-to-many relationship.
		/// </summary>
		/// <param name="entity">Entity</param>
		/// <returns></returns>
		public static async Task CheckManyToManyEligibilityAsync(this IOrganizationServiceAsync2 crm, string entity)
		{
			CanManyToManyRequest canManyToManyRequest = new()
			{
				EntityName = entity
			};

			var canManyToManyResponse = (CanManyToManyResponse)await crm.ExecuteAsync(canManyToManyRequest);

			if (!canManyToManyResponse.CanManyToMany)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {entity} cannot be part of a many-to-many relationship");
			}
		}


		public static async Task CheckManyToManyExplicitEligibilityAsync(this IOrganizationServiceAsync2 crm, string table1, string table2)
		{
			var request1 = new CanBeReferencedRequest
			{
				EntityName = table1
			};
			var response1 = (CanBeReferencedResponse)await crm.ExecuteAsync(request1);
			if (!response1.CanBeReferenced)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {table1} cannot be parent of an N-1 relationship");

			var request2 = new CanBeReferencedRequest
			{
				EntityName = table2
			};
			var response2 = (CanBeReferencedResponse)await crm.ExecuteAsync(request2);
			if (!response2.CanBeReferenced)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {table2} cannot be parent of an N-1 relationship");
		}


		/// <summary>
		/// Determines whether the given entities can participate in a many-to-one relationship.
		/// </summary>
		/// <param name="parentTable">The referenced table</param>
		/// <param name="childTable">The referencing table</param>
		/// <returns></returns>
		public static async Task CheckManyToOneEligibilityAsync(this IOrganizationServiceAsync2 crm, string parentTable, string childTable)
		{
			var request1 = new CanBeReferencedRequest
			{
				EntityName = parentTable
			};
			var response1 = (CanBeReferencedResponse)await crm.ExecuteAsync(request1);
			if (!response1.CanBeReferenced)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {parentTable} cannot be parent of an N-1 relationship");


			var request2 = new CanBeReferencingRequest()
			{
				EntityName = childTable
			};
			var response2 = (CanBeReferencingResponse)await crm.ExecuteAsync(request2);
			if (!response2.CanBeReferencing)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The entity {childTable} cannot be child of an N-1 relationship");
		}



		public static async Task<int> GetDefaultLanguageCodeAsync(this IOrganizationServiceAsync2 crm, CancellationToken? cancellationToken = null)
		{
			cancellationToken ??= CancellationToken.None;

			var query = new QueryExpression("organization")
			{
				ColumnSet = new ColumnSet("languagecode"),
				TopCount = 1,
				NoLock = true
			};

			var result = await crm.RetrieveMultipleAsync(query, cancellationToken.Value);

			if (result.Entities.Count == 0)
			{
				throw new CommandException(CommandException.XrmError, "Unable to retrieve the default language code. No organization found!");
			}

			var languageCode = result.Entities[0].GetAttributeValue<int>("languagecode");
			return languageCode;
		}



		public static async Task<EntityMetadata> GetEntityMetadataAsync(this IOrganizationServiceAsync2 crm, string entityName)
		{
			var request = new RetrieveEntityRequest
			{
				EntityFilters = EntityFilters.All,
				LogicalName = entityName,
			};

			var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
			return response.EntityMetadata;
		}




		public static string ToMarkdownCode(this string? text, string? defaultValue = null)
		{
			if (string.IsNullOrWhiteSpace(text)) return defaultValue ?? string.Empty;
			return $"`{text}`";
		}




		/// <summary>
		/// Returns the formatted value of a given entity attribute.
		/// </summary>
		/// <param name="entity">The entity</param>
		/// <param name="propertyName">The attribute to retrieve</param>
		/// <returns>The formatted value of the specified property</returns>
		public static string GetFormattedValue(this Entity entity, string propertyName)
		{
			return entity.FormattedValues.Contains(propertyName) ?
				entity.FormattedValues[propertyName] :
				entity.GetLiteralValue(propertyName);
		}

		/// <summary>
		/// Returns an aliased value providing the required casts.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="entity"></param>
		/// <param name="attributeLogicalName"></param>
		/// <returns></returns>
		public static T? GetAliasedValue<T>(this Entity entity, string attributeLogicalName)
		{
			if (null == entity.Attributes)
			{
				entity.Attributes = new AttributeCollection();
			}

			var value = entity.GetAttributeValue<AliasedValue>(attributeLogicalName);
			if (value?.Value == null) return default;
			
			return (T)value.Value;
		}

		/// <summary>
		/// Returns an aliased value providing the required casts.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="entity"></param>
		/// <param name="attributeLogicalName"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static T GetAliasedValue<T>(this Entity entity, string attributeLogicalName, string alias)
		{
			return GetAliasedValue<T>(entity, string.Format("{0}.{1}", alias, attributeLogicalName));
		}

		/// <summary>
		/// Converts the value of a property to its string representation
		/// </summary>
		/// <param name="entity">The entity</param>
		/// <param name="propertyName">The attribute to retrieve</param>
		/// <returns>The string representation of the specified property</returns>
		public static string GetLiteralValue(this Entity entity, string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName)) return string.Empty;

			entity.Attributes.TryGetValue(propertyName, out object oValue);

			return StringFromObject(oValue);
		}

		private static string StringFromObject(object oValue)
		{
			if (oValue is null) return string.Empty;

			if (oValue is EntityReference entityReference)
				return entityReference.Name;

			if (oValue is OptionSetValue optionSetValue)
				return optionSetValue.Value.ToString();

			if (oValue is Money money)
				return money.Value.ToString(CultureInfo.CurrentUICulture);

			if (oValue is AliasedValue aliasedValue)
				return StringFromObject(aliasedValue.Value);

			return oValue.ToString() ?? string.Empty;
		}


		/// <summary>
		/// Returns a new entity created merging the two provided ones.
		/// </summary>
		/// <param name="main">The original entity</param>
		/// <param name="delta">The entity to overlap</param>
		/// <returns>A new entity merged from the previous two</returns>
		public static Entity Merge(this Entity main, Entity delta)
		{
			var deltaName = delta != null ? delta.LogicalName : string.Empty;
			var deltaId = delta != null ? delta.Id : Guid.Empty;

			var entityName = main != null ? main.LogicalName : deltaName;
			var entityId = main != null ? main.Id : deltaId;

			var entity = new Entity(entityName) { Id = entityId };

			if (main != null)
			{
				foreach (var attribute in main.Attributes)
				{
					entity[attribute.Key] = attribute.Value;
				}
			}

			if (delta != null)
			{
				foreach (var attribute in delta.Attributes)
				{
					entity[attribute.Key] = attribute.Value;
				}
			}

			return entity;
		}



		public static Type? GetEnumType(this Type type)
		{
			if (type.IsEnum) return type;
			var u = Nullable.GetUnderlyingType(type);
			if (u != null && u.IsEnum) return u;
			return null;
		}
	}
}
