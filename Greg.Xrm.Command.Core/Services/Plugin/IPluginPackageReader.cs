namespace Greg.Xrm.Command.Services.Plugin
{
	public interface IPluginPackageReader
	{
		PluginPackageReadResult ReadPackageFile(string filePath);

		Task<PluginAssemblyReadResult> ReadAssemblyFileAsync(string filePath);
	}
}
