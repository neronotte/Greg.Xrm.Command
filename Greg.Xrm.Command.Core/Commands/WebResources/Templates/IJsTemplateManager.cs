namespace Greg.Xrm.Command.Commands.WebResources.Templates
{
	public interface IJsTemplateManager
	{
		Task<string> GetTemplateForAsync(JavascriptWebResourceType type, bool global);
	}
}