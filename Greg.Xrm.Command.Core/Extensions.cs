using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using System.Text;

namespace Greg.Xrm.Command
{
	public static class Extensions
	{
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
