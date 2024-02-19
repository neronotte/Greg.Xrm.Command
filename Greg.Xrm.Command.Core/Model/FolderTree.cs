namespace Greg.Xrm.Command.Model
{
    public static class FolderTree
	{
		public static DirectoryInfo CreateFolderTree(DirectoryInfo root, string[] folderTree)
		{
			if (folderTree == null || folderTree.Length == 0) return root;

			var current = root;
			for (var i = 0; i < folderTree.Length; i++) {

				var folderName = folderTree[i];
				var folder = current.GetDirectories(folderName).FirstOrDefault();
				if (folder == null) {
					folder = current.CreateSubdirectory(folderName);
				}

				current = folder;
			}
			return current;
		}
	}
}
