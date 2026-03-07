using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Greg.Xrm.Command.Updates;

namespace Greg.Xrm.Command.Commands.Config
{
	public class EnableAutoUpdateCommandExecutor(IOutput output, ISettingsRepository settings) : ICommandExecutor<EnableAutoUpdateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(EnableAutoUpdateCommand command, CancellationToken cancellationToken)
		{
			await settings.SetAsync(AutoUpdater.EnableAutoUpdateSettingKey, true);
			output.WriteLine("Auto update has been enabled.");
			return CommandResult.Success();
		}
	}

}
