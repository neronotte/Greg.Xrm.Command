using Greg.Xrm.Command.Commands.Solution.Extensions;

namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class EntityMetadataManager
	{
		public EntityMetadataManager(
			string entityDisplayName,
			string entityLogicalName,
			string entitySetName,
			bool isActivity,
			List<string> commonFields)
		{
			EntityDisplayName = entityDisplayName;
			EntityDisplayNameWithoutSpecialChar = entityDisplayName.Replace(" ", string.Empty).RemoveSpecialCharacters();
			EntityLogicalName = entityLogicalName;
			EntitySetName = entitySetName;
			IsActivity = isActivity;
			Attributes = new List<AttributeMetadataManager>();
			OptionSetAttributes = new List<AttributeMetadataManagerForPicklist>();
			CommonFields = commonFields;
		}

		public string EntityDisplayName { get; }
		public string EntityDisplayNameWithoutSpecialChar { get; }
		public string EntityLogicalName { get; }
		public string EntitySetName { get; }
		public bool IsActivity { get; }
		public List<AttributeMetadataManagerForPicklist> OptionSetAttributes { get; set; }
		public AttributeMetadataManagerForStatus? StatusAttribute { get; set; }
		public AttributeMetadataManagerForStatusReason? StatusReasonAttribute { get; set; }
		public List<AttributeMetadataManager> Attributes { get; set; }
		public List<string> CommonFields { get; }

		public void AddAttribute(AttributeMetadataManager attributeElem)
		{
			if (!CommonFields.Contains(attributeElem.LogicalNameConstant))
				Attributes.Add(attributeElem);

			if (attributeElem is AttributeMetadataManagerForPicklist picklistManager)
				OptionSetAttributes.Add(picklistManager);

			if (attributeElem is AttributeMetadataManagerForStatus statusManager)
				StatusAttribute = statusManager;

			if (attributeElem is AttributeMetadataManagerForStatusReason statusReasonManager)
				StatusReasonAttribute = statusReasonManager;
		}

		public string GetLastAttribute()
		{
			return Attributes.Count == 0 ? EntityLogicalName : Attributes[Attributes.Count - 1].LogicalNameConstant;
		}
	}
}
