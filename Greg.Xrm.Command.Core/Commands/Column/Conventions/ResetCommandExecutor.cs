using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Column.Conventions
{
	public class ResetCommandExecutor(
		IOutput output,
		ISettingsRepository settingsRepository
	) : ICommandExecutor<ResetCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ResetCommand command, CancellationToken cancellationToken)
		{
			output.Write("Resetting column naming conventions to default values...");
			try
			{
				var conventions = new ColumnConventions();
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
