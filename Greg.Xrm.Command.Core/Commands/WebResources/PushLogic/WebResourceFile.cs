using Greg.Xrm.Command.Model;

namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
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
