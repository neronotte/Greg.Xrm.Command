using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Output
{
	public static class ComponentEditorUrl
	{
		private const string DefaultSolutionId = "fd140aaf-4df4-11dd-bd17-0019b9312238";

		public static string? Create(string? environmentId, string tableName, string componentType, Guid componentId)
		{
			if (string.IsNullOrWhiteSpace(environmentId) || string.IsNullOrWhiteSpace(tableName) || componentId == Guid.Empty)
				return null;

			var editorPath = componentType == "form" ? "form/edit" : "view";
			return $"https://make.powerapps.com/e/{Uri.EscapeDataString(environmentId)}/s/{DefaultSolutionId}/entity/{Uri.EscapeDataString(tableName)}/{editorPath}/{componentId:D}";
		}

		public static void Write(IOutput output, IOrganizationServiceAsync2 crm, string tableName, string componentType, Guid componentId, string? name)
		{
			var environmentId = (crm as ServiceClient)?.EnvironmentId;
			var url = Create(environmentId, tableName, componentType, componentId);
			output.WriteLine(url == null
				? $"Editor URL unavailable for {componentType} '{name}'."
				: $"{componentType} '{name}': {url}");
		}
	}
}
