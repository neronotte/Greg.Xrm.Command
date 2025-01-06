using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Project;

namespace Greg.Xrm.Command.Commands.Projects
{
	public class SuspendProjectCommandExecutor(
		IOutput output,
		IPacxProjectRepository pacxProjectRepository
		)
		: ICommandExecutor<SuspendProjectCommand>
	{


		public async Task<CommandResult> ExecuteAsync(SuspendProjectCommand command, CancellationToken cancellationToken)
		{
			var project = await pacxProjectRepository.GetCurrentProjectAsync();
			if (project == null)
			{
				return CommandResult.Fail("The current folder does not belongs to a PACX project.");
			}

			if (project.IsSuspended)
			{
				output.WriteLine("The project is already suspended, nothing to do.");
				return CommandResult.Success();
			}

			project.IsSuspended = true;
			await pacxProjectRepository.SaveAsync(project, Environment.CurrentDirectory, cancellationToken);

			output.WriteLine("Project suspended!", ConsoleColor.Green);
			return CommandResult.Success();
		}
	}
}
