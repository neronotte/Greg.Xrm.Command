using Greg.Xrm.Command.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class SolutionDto
	{
		public string? UniqueName { get; set; }
		public string? FriendlyName { get; set; }
		public string? Version { get; set; }
		public bool IsVisible { get; set; }
		public bool IsManaged { get; set; }
		public string? PublisherUniqueName { get; set; }
		public string? PublisherFriendlyName { get; set; }
		public string? PublisherCustomizationPrefix { get; set; }

		public DateTime CreatedOn { get; set; }
		public DateTime ModifiedOn { get; set; }


		public static SolutionDto FromEntity(Entity entity)
		{
			return new SolutionDto
			{
				UniqueName = entity.GetAttributeValue<string>("uniquename"),
				FriendlyName = entity.GetAttributeValue<string>("friendlyname"),
				Version = entity.GetAttributeValue<string>("version"),
				IsVisible = entity.GetAttributeValue<bool>("isvisible"),
				IsManaged = entity.GetAttributeValue<bool>("ismanaged"),
				PublisherUniqueName = entity.GetAliasedValue<string>("p.uniquename"),
				PublisherFriendlyName = entity.GetAliasedValue<string>("p.friendlyname"),
				PublisherCustomizationPrefix = entity.GetAliasedValue<string>("p.customizationprefix"),
				CreatedOn = entity.GetAttributeValue<DateTime>("createdon"),
				ModifiedOn = entity.GetAttributeValue<DateTime>("modifiedon")
			};
		}
	}
}
