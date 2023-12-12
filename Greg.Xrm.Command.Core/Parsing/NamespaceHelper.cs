namespace Greg.Xrm.Command.Parsing
{
	public static class NamespaceHelper
	{
		public static INamespaceHelper Empty { get; } = new NamespaceHelperEmpty();

		class NamespaceHelperEmpty : NamespaceHelperBase
		{
			public NamespaceHelperEmpty() : base(string.Empty, Array.Empty<string>())
			{
			}
		}
	}
}
