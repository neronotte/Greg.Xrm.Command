namespace Greg.Xrm.Command.Services
{
    public static class FolderTree
    {
        public static DirectoryInfo CreateFolderTree(DirectoryInfo root, string[] folderTree)
        {
            if (folderTree == null || folderTree.Length == 0) return root;

            var current = root;
            for (var i = 0; i < folderTree.Length; i++)
            {

                var folderName = folderTree[i];
                var folder = current.GetDirectories(folderName).FirstOrDefault();
                if (folder == null)
                {
                    folder = current.CreateSubdirectory(folderName);
                }

                current = folder;
            }
            return current;
        }


        public static DirectoryInfo? RecurseBackFolderContainingFile(string fileName, DirectoryInfo? startDirectory = null)
        {
            if (startDirectory == null)
            {
                startDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            }

            var current = startDirectory;
            do
            {
                var file = current.GetFiles(fileName).FirstOrDefault();
				if (file != null)
				{
					return current;
				}

				current = current.Parent;
            }
            while (current != null);

            return null;
        }
    }
}
