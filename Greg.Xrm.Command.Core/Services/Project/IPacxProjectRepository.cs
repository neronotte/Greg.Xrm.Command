namespace Greg.Xrm.Command.Services.Project
{
	public interface IPacxProjectRepository
	{
		Task<PacxProjectDefinition?> GetCurrentProjectAsync();

		Task SaveAsync(PacxProjectDefinition projectDefinition, string folder, CancellationToken cancellationToken);
	}
}
