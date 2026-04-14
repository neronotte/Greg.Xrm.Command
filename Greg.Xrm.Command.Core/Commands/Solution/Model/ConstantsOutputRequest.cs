namespace Greg.Xrm.Command.Commands.Solution.Model
{
	public class ConstantsOutputRequest
	{
		public string SolutionName { get; init; } = string.Empty;
		public string? OutputCs { get; init; }
		public string? NamespaceCs { get; init; }
		public string? OutputJs { get; init; }
		public string? NamespaceJs { get; init; }
		public string? JsHeader { get; init; }
		public bool WithTypes { get; init; } = true;
		public bool WithDescriptions { get; init; } = true;
	}
}
