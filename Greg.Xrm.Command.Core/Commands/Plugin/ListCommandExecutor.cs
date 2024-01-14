
using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class ListCommandExecutor : ICommandExecutor<ListCommand>
	{
		private readonly IOutput output;
		private readonly IStorage storage;

		public ListCommandExecutor(IOutput output, IStorage storage)
		{
			this.output = output;
			this.storage = storage;
		}

		public Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			var storageFolder = this.storage.GetOrCreateStorageFolder();
			var pluginsRootFolder = storageFolder.CreateSubdirectory("Plugins");

			var subFolderList = pluginsRootFolder.GetDirectories().OrderBy(x => x.Name);


			var pluginInfoList = new List<PluginInfo>();
			foreach (var pluginFolder in subFolderList)
			{
				// need to check if the folder contains a plugin with the same name

				var pluginName = pluginFolder.Name;
				var pluginFile = pluginFolder.GetFiles(pluginName + ".dll");
				var versionFile = pluginFolder.GetFiles(".version");

				var isValid = pluginFile.Length == 1;
				var hasVersion = versionFile.Length == 1;
				string version = string.Empty;
				if (hasVersion)
				{
					version = File.ReadAllText(versionFile[0].FullName);
				}

				if (isValid)
				{
					pluginInfoList.Add(new PluginInfo
					{
						Name = pluginName,
						Version = version
					});
				}
			}

			if (pluginInfoList.Count == 0)
			{
				this.output.WriteLine("No plugins found.", ConsoleColor.Yellow);
				return Task.FromResult(CommandResult.Success());
			}

			this.output.WriteLine("Installed plugins:");
			foreach (var pluginInfo in pluginInfoList)
			{
				this.output.Write($"- {pluginInfo.Name}").Write(" (", ConsoleColor.DarkGray) ;
				if (string.IsNullOrWhiteSpace(pluginInfo.Version))
				{
					this.output.Write("local", ConsoleColor.DarkGray);
				}
				else
				{
					this.output.Write(pluginInfo.Version, ConsoleColor.DarkGray);
				}
				this.output.WriteLine(")", ConsoleColor.DarkGray);
			}
			return Task.FromResult(CommandResult.Success());
		}


		class PluginInfo
		{
			public required string Name { get; set; }

			public required string Version { get; set; }
		}
	}
}
