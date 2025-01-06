using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Project;

namespace Greg.Xrm.Command.Commands.Projects
{
	public class ResumeProjectCommandExecutor(
		IOutput output,
		IPacxProjectRepository pacxProjectRepository
		)
		: ICommandExecutor<ResumeProjectCommand>
	{


		public async Task<CommandResult> ExecuteAsync(ResumeProjectCommand command, CancellationToken cancellationToken)
		{
			var project = await pacxProjectRepository.GetCurrentProjectAsync();
			if (project == null)
			{
				return CommandResult.Fail("The current folder does not belongs to a PACX project.");
			}

			if (!project.IsSuspended)
			{
				output.WriteLine("The project is already enabled, nothing to do.");
				return CommandResult.Success();
			}

			project.IsSuspended = false;
			await pacxProjectRepository.SaveAsync(project, Environment.CurrentDirectory, cancellationToken);

			output.WriteLine("Project resumed!", ConsoleColor.Green);
			return CommandResult.Success();
		}
	}
}
