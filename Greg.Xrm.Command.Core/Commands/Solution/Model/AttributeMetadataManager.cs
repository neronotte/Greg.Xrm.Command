using Greg.Xrm.Command.Commands.Solution.Extensions;

namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class AttributeMetadataManager
	{
		public AttributeMetadataManager(
			string? entityLogicalName,
			string? displayName,
			string logicalName,
			string? type,
			string description)
		{
			EntityLogicalName = entityLogicalName;
			DisplayNameConstant = displayName;
			LogicalNameConstant = logicalName;
			LogicalNameConstantLabel = entityLogicalName == null || !entityLogicalName.Equals(logicalName)
				? logicalName
				: logicalName + "Field";
			Type = type;
			Description = description;
		}

		public string? EntityLogicalName { get; }
		public string? DisplayNameConstant { get; }
		public string LogicalNameConstant { get; }
		public string LogicalNameConstantLabel { get; }
		public string? Type { get; }
		public string Description { get; }

		public virtual IEnumerable<string> WriteFieldInfo()
		{
			yield break;
		}
	}
}
