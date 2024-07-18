namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	public interface IWebResourceFilesResolver
	{
		IReadOnlyList<WebResourceFile> ResolveFiles(WebResourceFolders folders);
	}
}
