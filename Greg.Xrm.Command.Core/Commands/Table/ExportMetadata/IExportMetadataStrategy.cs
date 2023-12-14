using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
    public interface IExportMetadataStrategy
    {
        Task<string?> ExportAsync(EntityMetadata entityMetadata, string outputFolder);
    }
}
