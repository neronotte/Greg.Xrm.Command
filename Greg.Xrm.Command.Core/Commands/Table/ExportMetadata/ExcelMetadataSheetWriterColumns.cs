using ClosedXML.Excel;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public class ExcelMetadataSheetWriterColumns : IExcelMetadataSheetWriter
	{
		public void Write(IXLWorkbook workbook, EntityMetadata entityMetadata)
		{
			var ws = workbook.Worksheets.Add("Columns");
			ws.ShowGridLines = false;

			ws.Range(1, 1, 1, 10)
				.Merge()
				.Title()
				.SetValue("Columns");

			ws.Cell(2, 1)
				.SetValue(entityMetadata.SchemaName)
				.Explanatory();

			var row = 4;

			ws.Range(row, 8, row, 11).Merge().TextAlign(XLAlignmentHorizontalValues.Center).LightOrange60().SetValue("String");
			ws.Cell(row, 13).TextAlign(XLAlignmentHorizontalValues.Center).LightGreen60().SetValue("Lookup");
			ws.Range(row, 14, row, 16).Merge().TextAlign(XLAlignmentHorizontalValues.Center).LightYellow60().SetValue("Picklist");
			ws.Range(row, 17, row, 19).Merge().TextAlign(XLAlignmentHorizontalValues.Center).LightBlue60().SetValue("Numeric");
			ws.Cell(row, 20).TextAlign(XLAlignmentHorizontalValues.Center).DarkBlue60().SetValue("Money");

			var col = 0;
			row++;

			ws.Cell(row, ++col).SetValue("Logical Name");
			ws.Cell(row, ++col).SetValue("Schema Name");
			ws.Cell(row, ++col).SetValue("Display Name");
			ws.Cell(row, ++col).SetValue("Description");
			ws.Cell(row, ++col).SetValue("Primary?");
			ws.Cell(row, ++col).SetValue("Required?");
			ws.Cell(row, ++col).SetValue("Type");

			// string
			ws.Cell(row, ++col).SetValue("Format");
			ws.Cell(row, ++col).SetValue("FormatName");
			ws.Cell(row, ++col).SetValue("Max Length");
			ws.Cell(row, ++col).SetValue("AutoNumber Format");
			ws.Cell(row, ++col).SetValue("Formula");

			// lookup
			ws.Cell(row, ++col).SetValue("Lookup Targets ");

			ws.Cell(row, ++col).SetValue("Picklist Options");
			ws.Cell(row, ++col).SetValue("Picklist Default Value");
			ws.Cell(row, ++col).SetValue("Is Global Picklist?");

			// numeric
			ws.Cell(row, ++col).SetValue("Min Value");
			ws.Cell(row, ++col).SetValue("Max Value");
			ws.Cell(row, ++col).SetValue("Precision");
			ws.Cell(row, ++col).SetValue("Precision Source");

			ws.Cell(row, ++col).SetValue("Is Audit Enabled?");
			ws.Cell(row, ++col).SetValue("FLS - Is Enabled?");
			ws.Cell(row, ++col).SetValue("FLS - Can Be Secured For Create?");
			ws.Cell(row, ++col).SetValue("FLS - Can Be Secured For Update?");
			ws.Cell(row, ++col).SetValue("FLS - Can Be Secured For Read?");

			var attributeRows = entityMetadata.Attributes
				.Where(x => x.AttributeType != AttributeTypeCode.Virtual)
				.Select(x => DataverseColumn.CreateFrom(x, entityMetadata))
				.OrderBy(x => x.LogicalName)
				.ToList();

			foreach (var attribute in attributeRows)
			{
				row++;
				col = 0;

				ws.Cell(row, ++col).SetValue(attribute.LogicalName);
				ws.Cell(row, ++col).SetValue(attribute.SchemaName);
				ws.Cell(row, ++col).SetValue(attribute.DisplayName);
				ws.Cell(row, ++col).SetValue(attribute.Description);
				ws.Cell(row, ++col).SetValue(attribute.PrimaryType);
				ws.Cell(row, ++col).SetValue(attribute.RequiredLevel);
				ws.Cell(row, ++col).SetValue(attribute.Type);
				ws.Cell(row, ++col).SetValue(attribute.Format);
				ws.Cell(row, ++col).SetValue(attribute.FormatName);
				ws.Cell(row, ++col).SetValue(attribute.MaxLength);
				ws.Cell(row, ++col).SetValue(attribute.AutoNumberFormat);
				ws.Cell(row, ++col).SetValue(attribute.Formula);
				ws.Cell(row, ++col).SetValue(attribute.LookupTargets);
				ws.Cell(row, ++col).SetValue(attribute.PicklistOptions);
				ws.Cell(row, ++col).SetValue(attribute.PicklistDefaultValue);
				ws.Cell(row, ++col).SetValue(attribute.IsGlobalPicklist);
				ws.Cell(row, ++col).SetValue(attribute.MinValue);
				ws.Cell(row, ++col).SetValue(attribute.MaxValue);
				ws.Cell(row, ++col).SetValue(attribute.Precision);
				ws.Cell(row, ++col).SetValue(attribute.PrecisionSource);
				ws.Cell(row, ++col).SetValue(attribute.IsAuditEnabled);
				ws.Cell(row, ++col).SetValue(attribute.IsFlsEnabled);
				ws.Cell(row, ++col).SetValue(attribute.IsFlsSecuredForCreate);
				ws.Cell(row, ++col).SetValue(attribute.IsFlsSecuredForUpdate);
				ws.Cell(row, ++col).SetValue(attribute.IsFlsSecuredForRead);
			}

			ws.CreateTable("Columns", 5, 1, row, col).EmphasizeFirstColumn = true;
			ws.Column(4).Width = 50;
		}
	}
}
