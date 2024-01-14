using Greg.Xrm.Command.Services;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class UninstallCommandExecutor : ICommandExecutor<UninstallCommand>
	{
		private readonly IOutput output;
		private readonly IStorage storage;

		public UninstallCommandExecutor(IOutput output, IStorage storage)
        {
			this.output = output;
			this.storage = storage;
		}


        public Task<CommandResult> ExecuteAsync(UninstallCommand command, CancellationToken cancellationToken)
		{
			var storageFolder = this.storage.GetOrCreateStorageFolder();
			var pluginRootFolder = storageFolder.CreateSubdirectory("Plugins");

			var pluginFolder = pluginRootFolder.GetDirectories(command.Name).FirstOrDefault();

			if (pluginFolder == null)
			{
				return Task.FromResult(CommandResult.Fail($"Plugin {command.Name} not found."));
			}

			try
			{
				this.output.Write($"Deleting plugin <{pluginFolder.Name}>...");
				
				File.WriteAllText(Path.Combine(pluginFolder.FullName, ".delete"), "Plugin deleted on " + DateTime.Now.ToLongDateString());

				this.output.WriteLine("Done", ConsoleColor.Green);

				return Task.FromResult(CommandResult.Success());
			}
			catch(Exception ex)
			{
				this.output.WriteLine("Failed", ConsoleColor.Red);
				return Task.FromResult(CommandResult.Fail(ex.Message));
			}
		}
	}
}
