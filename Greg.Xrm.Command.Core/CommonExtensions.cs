using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command
{
    public static class CommonExtensions
	{

		/// <summary>
		/// Clones a specific entity (excepts the standard attributes - createdon, ... - and the forbidden passed via parameter)
		/// </summary>
		/// <param name="original">The entity to clone</param>
		/// <param name="forbiddenAttributes">The attributes to not to clone</param>
		/// <returns>A new entity, cloned from the previous one</returns>
		public static Entity Clone(this Entity original, params string[] forbiddenAttributes)
		{
			var clone = new Entity(original.LogicalName);
			foreach (var attribute in original.Attributes)
			{
				if (!CloneSettings.IsForbidden(original, attribute.Key, forbiddenAttributes))
					clone[attribute.Key] = attribute.Value;
			}
			return clone;
		}


		public static string Join(this IEnumerable<string> parts, string separator)
		{
			return string.Join(separator, parts);
		}
	}
}
