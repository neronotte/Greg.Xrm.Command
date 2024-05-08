namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public interface IImportStrategy
	{
		Task<IReadOnlyList<IImportAction>> ImportAsync(CancellationToken cancellationToken);
	}
}
