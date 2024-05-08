

namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public class ImportStrategyFromExcel : IImportStrategy
	{
		private Stream stream;

		public ImportStrategyFromExcel(Stream stream)
		{
			this.stream = stream;
		}

		public Task<IReadOnlyList<IImportAction>> ImportAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}