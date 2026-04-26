using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Column.Conventions
{
	public class SetCommandExecutor(
		IOutput output,
		ISettingsRepository settingsRepository
	) : ICommandExecutor<SetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(SetCommand command, CancellationToken cancellationToken)
		{
			output.Write("Updating column naming conventions...");
			try
			{
				var conventions = await settingsRepository.GetAsync<ColumnConventions>(ColumnConventions.StorageKey) ?? new ColumnConventions();

				if (command.SimpleOptionSetSuffix != null)
				{
					conventions.SimpleOptionSetSuffix = command.SimpleOptionSetSuffix;
				}
				if (command.MultiselectOptionSetSuffix != null)
				{
					conventions.MultiselectOptionSetSuffix = command.MultiselectOptionSetSuffix;
				}
				if (command.Casing != null)
				{
					conventions.Casing = command.Casing.Value;
				}

				await settingsRepository.SetAsync(ColumnConventions.StorageKey, conventions);

				output.WriteLine("Done", ConsoleColor.Green);

				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
