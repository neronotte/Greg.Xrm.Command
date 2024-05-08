namespace Greg.Xrm.Command.Commands.Settings.Imports
{
	public class ImportStrategyFactory : IImportStrategyFactory
	{
		public async Task<IImportStrategy> CreateAsync(Stream stream, CancellationToken cancellationToken)
		{
			var format = await DetectFormatAsync(stream, cancellationToken);
			stream.Position = 0;

			switch (format)
			{
				case Format.Json:
					return new ImportStrategyFromJson(stream);
				case Format.Excel:
					return new ImportStrategyFromExcel(stream);
				default:
					throw new NotSupportedException($"The format {format} is not supported");
			}
		}
		private static async Task<Format> DetectFormatAsync(Stream stream, CancellationToken cancellationToken)
		{
			var buffer = new byte[1];
			var read = await stream.ReadAsync(buffer, cancellationToken);
			if (read == 0)
			{
				return Format.Text;
			}

			var firstChar = buffer[0];
			if (firstChar == (byte)'[')
			{
				return Format.Json;
			}

			return Format.Excel;
		}
	}
}
