namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class AttributeMetadataManagerForStatus : AttributeMetadataManager
	{
		public AttributeMetadataManagerForStatus(
			string? entityLogicalName,
			string? displayName,
			string logicalName,
			string? type,
			string description,
			Dictionary<int, string> optionSetValues)
			: base(entityLogicalName, displayName, logicalName, type, description)
		{
			StatusValues = optionSetValues ?? new Dictionary<int, string>();
		}

		public Dictionary<int, string> StatusValues { get; }

		public override IEnumerable<string> WriteFieldInfo()
		{
			yield return "\t\t/// Values:";
			foreach (var statusValue in StatusValues)
				yield return "\t\t" + string.Format("/// {0}: {1},", statusValue.Value, statusValue.Key);
		}
	}
}
