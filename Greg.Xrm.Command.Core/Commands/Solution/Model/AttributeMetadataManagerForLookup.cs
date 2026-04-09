namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class AttributeMetadataManagerForLookup : AttributeMetadataManager
	{
		public AttributeMetadataManagerForLookup(
			string? entityLogicalName,
			string? displayName,
			string logicalName,
			string? type,
			string description,
			List<string> targetEntities)
			: base(entityLogicalName, displayName, logicalName, type, description)
		{
			TargetEntities = targetEntities ?? new List<string>();
		}

		public List<string> TargetEntities { get; }

		public override IEnumerable<string> WriteFieldInfo()
		{
			string relatedEntityNames = string.Empty;
			TargetEntities.ForEach(ent => relatedEntityNames = relatedEntityNames + ent + ",");
			yield return "\t\t/// Related entities: " + relatedEntityNames;
		}
	}
}
