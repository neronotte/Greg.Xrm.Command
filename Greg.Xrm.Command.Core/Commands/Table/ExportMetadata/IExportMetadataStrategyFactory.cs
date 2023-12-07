namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
    public interface IExportMetadataStrategyFactory
    {
        IExportMetadataStrategy Create(ExportMetadataFormat format);
    }
}
