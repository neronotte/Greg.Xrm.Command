namespace Greg.Xrm.Command.Services.Plugin
{
	public class PluginPackageReadResult
	{
		public static PluginPackageReadResult Error(string message) => new()
		{
			ErrorMessage = message,
		};

		public static PluginPackageReadResult Success(string packageId, string packageVersion, string content) => new()
		{
			PackageId = packageId,
			PackageVersion = packageVersion,
			Content = content,
		};


		private PluginPackageReadResult()
		{
		}

		public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

		public string? ErrorMessage { get; init; }

		public string? PackageId { get; init; }
		public string? PackageVersion { get; init; }
		public string? Content { get; init; }
	}
}
