using System.Text;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	public class FolderResolver : IFolderResolver
	{
		public WebResourceFolders ResolveFrom(string? path, string publisherPrefix)
		{
			if (string.IsNullOrWhiteSpace(publisherPrefix))
				throw new ArgumentNullException(nameof(publisherPrefix));


			path ??= Environment.CurrentDirectory;
			string? rootFolder;


			// if the path ends with \\**, we remove it
			// we want to avoid paths ending with a double **
			if (path.EndsWith(Path.DirectorySeparatorChar + "**"))
			{
				path = path[0..^3];
			}

			// if the path starts with $
			// e.g. $ or $\greg_\folder\file.txt
			// then we need to resolve the root folder from the current directory, and there infer the
			// target path relative to the root folder
			if (path.StartsWith("$", StringComparison.OrdinalIgnoreCase))
			{
				rootFolder = FindRootProjectFolder(Environment.CurrentDirectory);
				if (rootFolder == null)
				{
					throw new ArgumentException("No WebResource project root folder was found. The current command works only in the context of a WebResource project created via pacx solution init.", nameof(path));
				}

				path = new StringBuilder(rootFolder)
					.Append(Path.DirectorySeparatorChar)
					.Append(path[1..].TrimStart(Path.DirectorySeparatorChar))
					.ToString();
			}
			else
			{
				// otherwise, if the path is fully qualified, we just use it

				if (!Path.IsPathFullyQualified(path))
				{
					// if not, we convert the relative path to an absolute path
					path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
				}

				// and then we calculate the root folder recoursing up from the path
				rootFolder = FindRootProjectFolder(path);
				if (rootFolder == null)
				{
					throw new ArgumentException("No WebResource project root folder was found. The current command works only in the context of a WebResource project created via pacx solution init.", nameof(path));
				}
			}

			return new WebResourceFolders(rootFolder.TrimEnd(Path.DirectorySeparatorChar), path.TrimEnd(Path.DirectorySeparatorChar), publisherPrefix);
		}

		public static string? FindRootProjectFolder(string currentPath)
		{
			if (currentPath == null)
				throw new ArgumentNullException(nameof(currentPath));

			if (currentPath.EndsWith(Path.DirectorySeparatorChar))
				currentPath = currentPath[0..^1];

			var wildcardIndex = currentPath.IndexOf("**");
			if (wildcardIndex >= 0)
			{
				currentPath = currentPath.Substring(0, wildcardIndex);
			}
			if (!Directory.Exists(currentPath))
			{
				currentPath = Path.GetDirectoryName(currentPath) ?? currentPath;
			}


			var directory = new DirectoryInfo(currentPath);
			while (directory != null && directory.Exists)
			{
				if (directory.GetFiles("*.wr.pacx").Any())
				{
					return directory.FullName;
				}

				directory = directory.Parent;
			}

			return null;
		}
	}
}
