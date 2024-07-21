namespace Greg.Xrm.Command.Commands.WebResources.Templates
{
	public interface IJsTemplateManager
	{
		Task<string> GetTemplateForAsync(JavascriptWebResourceType type, bool global);

		Task SetTemplateForAsync(JavascriptWebResourceType type, bool global, string template);

		Task ResetTemplateForAsync(JavascriptWebResourceType type, bool global);
	}
}