using ClosedXML.Excel;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public interface IExcelMetadataSheetWriter
	{
		void Write(IXLWorkbook workbook, EntityMetadata entityMetadata);
	}
}
