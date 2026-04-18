namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class AttributeMetadataManagerForStatusReason : AttributeMetadataManager
	{
		public AttributeMetadataManagerForStatusReason(
			string? entityLogicalName,
			string? displayName,
			string logicalName,
			string? type,
			string description,
			List<Tuple<int, int, string>> statusValues)
			: base(entityLogicalName, displayName, logicalName, type, description)
		{
			StatusReasonValues = statusValues ?? new List<Tuple<int, int, string>>();
		}

		public List<Tuple<int, int, string>> StatusReasonValues { get; set; }

		public override IEnumerable<string> WriteFieldInfo()
		{
			yield return "\t\t/// Values:";
			foreach (var statusReasonValue in StatusReasonValues)
				yield return $"\t\t/// {statusReasonValue.Item3}: {statusReasonValue.Item2},";
		}
	}
}
