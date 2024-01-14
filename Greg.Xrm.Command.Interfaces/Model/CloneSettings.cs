using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Model
{
	/// <summary>
	/// Settings that can be used to configure the clonation feature
	/// </summary>
	public static class CloneSettings
	{
		private static readonly List<string> ForbiddenAttributes = new();

		static CloneSettings()
		{
			ForbiddenAttributes.AddRange(new[]
			{
					"statecode",
					"statuscode",
					"ownerid",
					"owningbusinessunit",
					"owningteam",
					"owninguser",
					"createdon",
					"createdby",
					"modifiedon",
					"modifiedby"
				});
		}


		/// <summary>
		/// Indicates whether a property is forbidden or not for the clone.
		/// </summary>
		/// <param name="original">The entity that contains the property to clone</param>
		/// <param name="propertyName">The name of the property to clone</param>
		/// <param name="otherForbiddenAttributes">Forbidden attributes</param>
		/// <returns></returns>
		public static bool IsForbidden(Entity original, string propertyName, string[] otherForbiddenAttributes)
		{
			otherForbiddenAttributes ??= Array.Empty<string>();
			
			if (string.Equals(propertyName, original.LogicalName + "id", StringComparison.OrdinalIgnoreCase)) return true;
			if (ForbiddenAttributes.Exists(x => string.Equals(x, propertyName, StringComparison.OrdinalIgnoreCase))) return true;
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
			return otherForbiddenAttributes.Any(x => string.Equals(x, propertyName, StringComparison.OrdinalIgnoreCase));
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
		}
	}
}
