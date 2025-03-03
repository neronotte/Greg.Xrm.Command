namespace Greg.Xrm.Command.Commands.WebResources.ProjectFile
{
	public interface IWebResourceProjectFileRepository
	{
		Task<(bool, ProjectFileV1)> TryReadAsync(DirectoryInfo folder);
		Task SaveAsync(DirectoryInfo folder, string publisherPrefix, ProjectFileV1? projectFile = null);
	}
}
