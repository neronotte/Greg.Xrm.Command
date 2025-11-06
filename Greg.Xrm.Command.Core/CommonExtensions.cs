using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.Text;

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


		public static string Dump(this FaultException<OrganizationServiceFault> ex)
		{
			var sb = new StringBuilder();
			sb.Append($"OrganizationServiceFault: ").Append(ex.Message);
			if (ex.Detail != null)
			{
				sb.AppendLine();
				sb.Append($"  ErrorCode: {ex.Detail.ErrorCode}").AppendLine();
				sb.Append($"  Message: {ex.Detail.Message}").AppendLine();
				sb.Append($"  Timestamp: {ex.Detail.Timestamp}").AppendLine();
				sb.Append($"  InnerFault: {ex.Detail.InnerFault}").AppendLine();
				sb.Append($"  TraceText: {ex.Detail.TraceText}").AppendLine();
			}
			return sb.ToString();
		}
	}
}
