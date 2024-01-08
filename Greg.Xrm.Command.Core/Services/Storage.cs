namespace Greg.Xrm.Command.Services
{
	public class Storage : IStorage
	{
		private DirectoryInfo? storageFolder = null;

		public DirectoryInfo GetOrCreateStorageFolder()
		{
			if (this.storageFolder != null) 
				return this.storageFolder;

			var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			folderPath = Path.Combine(folderPath, "Greg.Xrm.Command");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}



			this.storageFolder = new DirectoryInfo(folderPath);
			return this.storageFolder;
		}
	}
}
