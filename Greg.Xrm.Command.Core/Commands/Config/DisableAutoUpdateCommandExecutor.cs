using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Greg.Xrm.Command.Updates;

namespace Greg.Xrm.Command.Commands.Config
{
	public class DisableAutoUpdateCommandExecutor(IOutput output, ISettingsRepository settings) : ICommandExecutor<DisableAutoUpdateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(DisableAutoUpdateCommand command, CancellationToken cancellationToken)
		{
			await settings.SetAsync(AutoUpdater.EnableAutoUpdateSettingKey, false);
			output.WriteLine("Auto update has been disabled.");
			return CommandResult.Success();
		}
	}

}
