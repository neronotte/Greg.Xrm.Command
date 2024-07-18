using System.Text;
using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{

    public class WebResourceFilesResolver : IWebResourceFilesResolver
	{
		public const string DataXmlToken = ".data.xml";

		public IReadOnlyList<WebResourceFile> ResolveFiles(WebResourceFolders folders)
		{
			if (folders == null)
				throw new ArgumentNullException(nameof(folders));

			if (IsWellDefinedFile(folders.RequestedPath))
			{
				if (folders.RequestedPath.EndsWith(DataXmlToken, StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException($"You selected directly a {DataXmlToken} file, that should not be pushed into the Dataverse.", nameof(folders));
				}

				return new[] { CreateWebResourceFrom(folders.RequestedPath, folders.ProjectRootPath) };
			}

			if (IsWellDefinedFolder(folders.RequestedPath))
			{
				var path = folders.RequestedPath;
				if (string.Equals(path, folders.ProjectRootPath))
				{
					path = Path.Combine(path, folders.PublisherPrefix + "_");

					var files1 = ResolveFilesFromFolder(new DirectoryInfo(path), folders.ProjectRootPath);
					var files2 = ResolveFilesFromFolder(new DirectoryInfo(folders.ProjectRootPath), folders.ProjectRootPath, false, folders.PublisherPrefix + "_*");
					
					return files1.Union(files2).OrderBy(x => x.RemotePath).ToList();
				}

				return ResolveFilesFromFolder(new DirectoryInfo(path), folders.ProjectRootPath);
			}

			// if here, it means that there is a wildcard

			var directory = Path.GetDirectoryName(folders.RequestedPath) ?? string.Empty;
			var recourse = directory.EndsWith("**");
			if (recourse) directory = Path.GetDirectoryName(directory) ?? string.Empty;
			var searchPattern = Path.GetFileName(folders.RequestedPath);

			return ResolveFilesFromFolder(new DirectoryInfo(directory), folders.ProjectRootPath, recourse, searchPattern);
		}

		public static IReadOnlyList<WebResourceFile> ResolveFilesFromFolder(DirectoryInfo folder, string rootFolder, bool recourse = true, string? searchPattern = null)
		{
			var list = new List<WebResourceFile>();
			if (!folder.Exists) return list;

			var fileList = searchPattern == null ? folder.GetFiles() : folder.GetFiles(searchPattern);
			foreach (var file in fileList)
			{
				var fileType = WebResource.GetTypeFromExtension(file.FullName);
				if (fileType == null)
				{
					continue;
				}

				list.Add(new WebResourceFile(file.FullName, rootFolder, fileType ?? WebResourceType.Data));
			}

			if (recourse)
			{
				foreach (var directory in folder.GetDirectories())
				{
					var subList = ResolveFilesFromFolder(directory, rootFolder, recourse);
					list.AddRange(subList);
				}
			}
			list.Sort((a, b) => string.Compare(a.RemotePath, b.RemotePath, StringComparison.Ordinal));
			return list;
		}

		public static WebResourceFile CreateWebResourceFrom(string path, string rootFolder)
		{
			var file = new FileInfo(path);
			if (!file.Exists)
			{
				throw new FileNotFoundException($"The file {path} was not found.");
			}

			var type = WebResource.GetTypeFromExtension(file.FullName);
			if (type == null)
			{
				throw new NotSupportedException($"The file {path} has an extension that is not supported.");
			}

			return new WebResourceFile(path, rootFolder, type ?? WebResourceType.Data);
		}

		public static bool IsWellDefinedFile(string folder)
		{
			return File.Exists(folder);
		}

		public static bool IsWellDefinedFolder(string folder)
		{
			return Directory.Exists(folder);
		}

		public static string? FindRootProjectFolder(string currentDirectory)
		{
			var directory = new DirectoryInfo(currentDirectory);
			while (directory != null)
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


	public class WebResourceFile
	{
        public WebResourceFile(string localPath, string rootFolder, WebResourceType type)
		{
			this.LocalPath = localPath;
			this.RemotePath = localPath.Substring(rootFolder.Length).TrimStart(Path.DirectorySeparatorChar).Replace("\\", "/");
			this.Type = type;
		}

        public string LocalPath { get; }
		public string RemotePath { get; }
		public WebResourceType Type { get; }
	}
}
