namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class AttributeMetadataManagerForPicklist : AttributeMetadataManager
	{
		public AttributeMetadataManagerForPicklist(
			string? entityLogicalName,
			string? displayName,
			string logicalName,
			string? type,
			string description,
			Dictionary<int, string> optionSetValues,
			string? globalOptionSetLogicalName)
			: base(entityLogicalName, displayName, logicalName, type, description)
		{
			PicklistValues = optionSetValues ?? new Dictionary<int, string>();
			GlobalOptionSetLogicalName = globalOptionSetLogicalName;
		}

		public Dictionary<int, string> PicklistValues { get; }
		public string? GlobalOptionSetLogicalName { get; }

		public override IEnumerable<string> WriteFieldInfo()
		{
			yield return "\t\t/// Values:";
			foreach (var picklistValue in PicklistValues)
				yield return "\t\t" + string.Format("/// {0}: {1},", picklistValue.Value, picklistValue.Key);
		}
	}
}
