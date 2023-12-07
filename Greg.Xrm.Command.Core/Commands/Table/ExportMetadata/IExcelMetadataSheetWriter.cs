using Microsoft.Xrm.Sdk.Metadata;
using OfficeOpenXml;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public interface IExcelMetadataSheetWriter
	{
		void Write(ExcelPackage package, EntityMetadata entityMetadata);
	}
}
