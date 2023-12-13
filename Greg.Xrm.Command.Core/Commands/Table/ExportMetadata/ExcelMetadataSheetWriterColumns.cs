using Microsoft.Xrm.Sdk.Metadata;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public class ExcelMetadataSheetWriterColumns : IExcelMetadataSheetWriter
	{


		


		public void Write(ExcelPackage package, EntityMetadata entityMetadata)
		{
			var ws = package.Workbook.Worksheets.Add("Columns");
			ws.View.ShowGridLines = false;

			ws.Cells[1, 1, 1, 10]
				.MergeCells()
				.SetValue("Columns")
				.Title();

			ws.Cells[2, 1]
				.SetValue(entityMetadata.SchemaName)
				.Explanatory();

			var row = 4;

			ws.Cells[row, 8, row, 11].SetValue("String").MergeCells().TextAlign(ExcelHorizontalAlignment.Center).LightOrange60();
			ws.Cells[row, 13].SetValue("Lookup").MergeCells().TextAlign(ExcelHorizontalAlignment.Center).LightGreen60();
			ws.Cells[row, 14, row, 16].SetValue("Picklist").MergeCells().TextAlign(ExcelHorizontalAlignment.Center).LightYellow60();
			ws.Cells[row, 17, row, 19].SetValue("Numeric").MergeCells().TextAlign(ExcelHorizontalAlignment.Center).LightBlue60();
			ws.Cells[row, 20].SetValue("Money").MergeCells().TextAlign(ExcelHorizontalAlignment.Center).DarkBlue60();

			var col = 0;
			row++;

			ws.Cells[row, ++col].SetValue("Logical Name");
			ws.Cells[row, ++col].SetValue("Schema Name");
			ws.Cells[row, ++col].SetValue("Display Name");
			ws.Cells[row, ++col].SetValue("Description");
			ws.Cells[row, ++col].SetValue("Primary?");
			ws.Cells[row, ++col].SetValue("Required?");
			ws.Cells[row, ++col].SetValue("Type");

			// string
			ws.Cells[row, ++col].SetValue("Format");
			ws.Cells[row, ++col].SetValue("FormatName");
			ws.Cells[row, ++col].SetValue("Max Length");
			ws.Cells[row, ++col].SetValue("AutoNumber Format");
			ws.Cells[row, ++col].SetValue("Formula");

			// lookup
			ws.Cells[row, ++col].SetValue("Lookup Targets ");

			ws.Cells[row, ++col].SetValue("Picklist Options");
			ws.Cells[row, ++col].SetValue("Picklist Default Value");
			ws.Cells[row, ++col].SetValue("Is Global Picklist?");

			// numeric
			ws.Cells[row, ++col].SetValue("Min Value");
			ws.Cells[row, ++col].SetValue("Max Value");
			ws.Cells[row, ++col].SetValue("Precision");
			ws.Cells[row, ++col].SetValue("Precision Source");

			ws.Cells[row, ++col].SetValue("Is Audit Enabled?");
			ws.Cells[row, ++col].SetValue("FLS - Is Enabled?");
			ws.Cells[row, ++col].SetValue("FLS - Can Be Secured For Create?");
			ws.Cells[row, ++col].SetValue("FLS - Can Be Secured For Update?");
			ws.Cells[row, ++col].SetValue("FLS - Can Be Secured For Read?");

			var attributeRows = entityMetadata.Attributes
				.Where(x => x.AttributeType != AttributeTypeCode.Virtual)
				.Select(x => DataverseColumn.CreateFrom(x, entityMetadata))
				.OrderBy(x => x.LogicalName)
				.ToList();

			foreach (var attribute in attributeRows)
			{
				row++;
				col = 0;

				ws.Cells[row, ++col].SetValue(attribute.LogicalName);
				ws.Cells[row, ++col].SetValue(attribute.SchemaName);
				ws.Cells[row, ++col].SetValue(attribute.DisplayName);
				ws.Cells[row, ++col].SetValue(attribute.Description);
				ws.Cells[row, ++col].SetValue(attribute.PrimaryType);
				ws.Cells[row, ++col].SetValue(attribute.RequiredLevel);
				ws.Cells[row, ++col].SetValue(attribute.Type);
				ws.Cells[row, ++col].SetValue(attribute.Format);
				ws.Cells[row, ++col].SetValue(attribute.FormatName);
				ws.Cells[row, ++col].SetValue(attribute.MaxLength);
				ws.Cells[row, ++col].SetValue(attribute.AutoNumberFormat);
				ws.Cells[row, ++col].SetValue(attribute.Formula);
				ws.Cells[row, ++col].SetValue(attribute.LookupTargets);
				ws.Cells[row, ++col].SetValue(attribute.PicklistOptions);
				ws.Cells[row, ++col].SetValue(attribute.PicklistDefaultValue);
				ws.Cells[row, ++col].SetValue(attribute.IsGlobalPicklist);
				ws.Cells[row, ++col].SetValue(attribute.MinValue);
				ws.Cells[row, ++col].SetValue(attribute.MaxValue);
				ws.Cells[row, ++col].SetValue(attribute.Precision);
				ws.Cells[row, ++col].SetValue(attribute.PrecisionSource);
				ws.Cells[row, ++col].SetValue(attribute.IsAuditEnabled);
				ws.Cells[row, ++col].SetValue(attribute.IsFlsEnabled);
				ws.Cells[row, ++col].SetValue(attribute.IsFlsSecuredForCreate);
				ws.Cells[row, ++col].SetValue(attribute.IsFlsSecuredForUpdate);
				ws.Cells[row, ++col].SetValue(attribute.IsFlsSecuredForRead);
			}

			ws.CreateTable("Columns", 5, 1, row, col).ShowFirstColumn = true;
			ws.Column(4).Width = 50;
		}

		


	}
}
