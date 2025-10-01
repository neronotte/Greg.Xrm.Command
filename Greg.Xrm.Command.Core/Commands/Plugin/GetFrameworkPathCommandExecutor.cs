using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class GetFrameworkPathCommandExecutor(
		IOutput output,
		ISettingsRepository settingsRepository) : ICommandExecutor<GetFrameworkPathCommand>
	{

		public async Task<CommandResult> ExecuteAsync(GetFrameworkPathCommand command, CancellationToken cancellationToken)
		{
			var value = await settingsRepository.GetAsync<string>(Services.Plugin.Constants.FrameworkPathKey);
			if (string.IsNullOrWhiteSpace(value))
			{
				value = Services.Plugin.Constants.DefaultFrameworkPath;
			}

			output.Write($"The .NET Framework 4.6.2 reference assemblies path is: ");
			output.WriteLine(value, ConsoleColor.Green);

			return CommandResult.Success();
		}
	}
}
