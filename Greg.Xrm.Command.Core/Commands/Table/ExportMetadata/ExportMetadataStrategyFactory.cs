using Greg.Xrm.Command.Services.Output;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
    public class ExportMetadataStrategyFactory : IExportMetadataStrategyFactory
    {
        private readonly IOutput output;

        public ExportMetadataStrategyFactory(IOutput output)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }


        public IExportMetadataStrategy Create(ExportMetadataFormat format)
        {
            switch (format)
            {
                case ExportMetadataFormat.Json:
                    return new ExportMetadataStrategyJson(output);
                case ExportMetadataFormat.Excel:
                    return new ExportMetadataStrategyExcel(output);
                default:
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The format '{format}' is not supported.");
            }
        }
    }
}
