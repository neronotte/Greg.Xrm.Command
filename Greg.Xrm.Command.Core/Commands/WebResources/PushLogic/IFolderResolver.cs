namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	public interface IFolderResolver
	{
		WebResourceFolders ResolveFrom(string? path, string publisherPrefix);
	}
}
