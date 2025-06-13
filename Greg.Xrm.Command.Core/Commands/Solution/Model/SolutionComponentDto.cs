using Greg.Xrm.Command.Model;

namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class SolutionComponentDto
	{
		public string? ComponentType { get; set; }
		public int ComponentTypeCode { get; set; }

		public string? ObjectId { get; set; }

		public string? Label { get; set; }


		public static SolutionComponentDto FromEntity(SolutionComponent entity)
		{
			return new SolutionComponentDto
			{
				ComponentType = entity.TypeLabel ?? entity.ComponentTypeName,
				ComponentTypeCode = entity.componenttype?.Value ?? 0,
				ObjectId = entity.objectid.ToString(),
				Label = entity.Label
			};
		}
	}
}
