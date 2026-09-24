namespace Greg.Xrm.Command.Commands.Forms
{
	internal static class FormXmlFileHelper
	{
		public static bool TryValidateOutputPath(string path, out string? error)
		{
			try
			{
				var folder = Path.GetDirectoryName(Path.GetFullPath(path));
				if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
				{
					error = $"The folder <{folder}> does not exist.";
					return false;
				}
			}
			catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or IOException)
			{
				error = $"The output file path <{path}> is not valid: {ex.Message}";
				return false;
			}

			error = null;
			return true;
		}
	}
}
