using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Project;

namespace Greg.Xrm.Command.Commands.Projects
{
	public class InfoProjectCommandExecutor(
		IOutput output,
		IPacxProjectRepository pacxProjectRepository
		)
		: ICommandExecutor<InfoProjectCommand>
	{


		public async Task<CommandResult> ExecuteAsync(InfoProjectCommand command, CancellationToken cancellationToken)
		{
			var project = await pacxProjectRepository.GetCurrentProjectAsync();
			if (project == null)
			{
				output.WriteLine("The current folder does not belongs to a PACX project.");
				return CommandResult.Success();
			}

			output.Write($"  Version      : ").WriteLine(project.Version, ConsoleColor.Cyan);
			output.Write($"  Auth. Profile: ").WriteLine(project.AuthProfileName, ConsoleColor.Cyan);
			output.Write($"  Def. Solution: ").WriteLine(project.SolutionName, ConsoleColor.Cyan);
			output.Write($"  Suspended    : ").WriteLine(project.IsSuspended, ConsoleColor.Cyan);

			return CommandResult.Success();
		}
	}
}
