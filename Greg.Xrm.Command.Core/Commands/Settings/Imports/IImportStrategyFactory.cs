namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public interface IImportStrategyFactory
	{
		Task<IImportStrategy> CreateAsync(Stream stream, CancellationToken cancellationToken);
	}
}
