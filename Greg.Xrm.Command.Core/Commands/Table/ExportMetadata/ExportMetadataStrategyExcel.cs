using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Metadata;
using OfficeOpenXml;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
    public class ExportMetadataStrategyExcel : IExportMetadataStrategy
    {
        private readonly IOutput output;
        private readonly List<IExcelMetadataSheetWriter> sheetWriterList = new();

        public ExportMetadataStrategyExcel(IOutput output)
        {
            this.output = output;
			this.sheetWriterList.Add(new ExcelMetadataSheetWriterTable());
			this.sheetWriterList.Add(new ExcelMetadataSheetWriterColumns());
		}


        public async Task<string?> ExportAsync(EntityMetadata entityMetadata, string outputFolder)
        {
            var fileName = $"{entityMetadata.SchemaName}.xlsx";
            var filePath = Path.Combine(outputFolder, fileName);



            try
            {
                using var package = new ExcelPackage();

                foreach (var writer in this.sheetWriterList)
                {
                    writer.Write(package, entityMetadata);
                }

                await package.SaveAsAsync(filePath);
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