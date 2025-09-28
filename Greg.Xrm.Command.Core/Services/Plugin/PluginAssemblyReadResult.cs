namespace Greg.Xrm.Command.Services.Plugin
{
	public class PluginAssemblyReadResult
	{
		public static PluginAssemblyReadResult Error(string message) => new()
		{
			ErrorMessage = message,
			PluginTypes = []
		};

		public static PluginAssemblyReadResult Success(AssemblyProperties properties, string content, string[] pluginTypes) => new()
		{
			AssemblyProperties = properties,
			Content = content,
			PluginTypes = pluginTypes ?? []
		};


		private PluginAssemblyReadResult()
		{
		}

		public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

		public string? ErrorMessage { get; init; }

		public AssemblyProperties? AssemblyProperties { get; init; }
		public string? Content { get; init; }

		public string[] PluginTypes { get; init; }
	}
}
