using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Metadata;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
    public class ExportMetadataStrategyJson : IExportMetadataStrategy
    {
        private readonly IOutput output;

        public ExportMetadataStrategyJson(IOutput output)
        {
            this.output = output;
        }


        public async Task<string?> ExportAsync(EntityMetadata entityMetadata, string outputFolder)
        {
            var text = JsonConvert.SerializeObject(entityMetadata, Formatting.Indented);


            var fileName = $"{entityMetadata.SchemaName}.json";
            var filePath = Path.Combine(outputFolder, fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, text);

                return filePath;
            }
            catch (Exception ex)
            {
                output.WriteLine()
                    .Write("Error while trying to write on the generated file: ", ConsoleColor.Red)
                    .WriteLine(ex.Message, ConsoleColor.Red);

                return null;
            }
        }
    }
}
