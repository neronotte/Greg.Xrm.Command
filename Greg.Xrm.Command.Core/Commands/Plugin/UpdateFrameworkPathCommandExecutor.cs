using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class UpdateFrameworkPathCommandExecutor(
		IOutput output, 
		ISettingsRepository settingsRepository) : ICommandExecutor<UpdateFrameworkPathCommand>
	{
		
		public async Task<CommandResult> ExecuteAsync(UpdateFrameworkPathCommand command, CancellationToken cancellationToken)
		{
			string newPath = command.NewPath.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)
				? Services.Plugin.Constants.DefaultFrameworkPath
				: new DirectoryInfo(command.NewPath).FullName;

			await settingsRepository.SetAsync(Services.Plugin.Constants.FrameworkPathKey, newPath);

			output.Write($"The .NET Framework 4.6.2 reference assemblies path has been updated to: ");
			output.WriteLine(newPath, ConsoleColor.Green);

			return CommandResult.Success();
		}
	}
}
