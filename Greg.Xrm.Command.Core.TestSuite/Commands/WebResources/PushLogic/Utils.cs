namespace Greg.Xrm.Command.Commands.WebResources.PushLogic
{
	public static class Utils
	{
		public static string CreateTempFolder()
		{
			var temp = Path.Combine(Path.GetTempPath(), "PACX-" + DateTime.Now.Ticks);
			CreateFolder(temp);
			return temp;
		}
		public static string CreateLocalTempFolder()
		{
			var temp = Path.Combine(Environment.CurrentDirectory, "PACX-" + DateTime.Now.Ticks);
			CreateFolder(temp);
			return temp;
		}

		public static void CreateFolder(string root, string? path = null)
		{
			if (path != null)
			{
				root = Path.Combine(root, path);
			}

			Directory.CreateDirectory(root);
		}

		public static void CreateFile(string root, string filePath, string? content = "")
		{
			var fullPath = Path.Combine(root, filePath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
			File.WriteAllText(fullPath, content);
		}

		public static void DeleteFolder(string root, string ? path = null)
		{
			if (path != null)
			{
				root = Path.Combine(root, path);
			}

			Directory.Delete(root, true);
		}
	}
}
