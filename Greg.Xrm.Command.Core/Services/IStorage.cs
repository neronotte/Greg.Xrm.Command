namespace Greg.Xrm.Command.Services
{
	public interface IStorage
	{
		DirectoryInfo GetOrCreateStorageFolder();
	}
}
