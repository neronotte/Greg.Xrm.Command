using Greg.Xrm.Command.Commands.Settings.Imports;
using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Settings
{
    public class ImportCommandExecutor : ICommandExecutor<ImportCommand>
	{
		private readonly IOutput output;
		private readonly IImportStrategyFactory importStrategyFactory;

		public ImportCommandExecutor(
			IOutput output,
			IImportStrategyFactory importStrategyFactory
			)
        {
			this.output = output;
			this.importStrategyFactory = importStrategyFactory;
		}


        public async Task<CommandResult> ExecuteAsync(ImportCommand command, CancellationToken cancellationToken)
		{
			var file = new FileInfo(command.FileName);
			if (!file.Exists)
			{
				return CommandResult.Fail($"The file {command.FileName} does not exists");
			}



			this.output.Write("Detecting file format...");
			IImportStrategy importStrategy;
			try
			{
				using var stream = file.OpenRead();
				importStrategy = await this.importStrategyFactory.CreateAsync(stream, cancellationToken);
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while reading content of file <{command.FileName}>: {ex.Message}");
			}




			this.output.Write("Parsing file content...");
			IReadOnlyList<IImportAction> actions;
			try
			{
				actions = await importStrategy.ImportAsync(cancellationToken);
				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error while parsing content of file <{command.FileName}>: {ex.Message}");

			}





			return CommandResult.Success();
		}
	}
}
