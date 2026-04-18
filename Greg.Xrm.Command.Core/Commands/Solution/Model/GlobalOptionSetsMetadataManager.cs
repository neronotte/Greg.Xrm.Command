namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class GlobalOptionSetsMetadataManager
	{
		public GlobalOptionSetsMetadataManager(
			string displayName,
			string logicalName,
			Dictionary<int, string> optionSetValues)
		{
			DisplayName = displayName;
			LogicalName = logicalName;
			PickListValues = optionSetValues ?? new Dictionary<int, string>();
		}

		public string DisplayName { get; }
		public string LogicalName { get; }
		public Dictionary<int, string> PickListValues { get; }
	}
}
