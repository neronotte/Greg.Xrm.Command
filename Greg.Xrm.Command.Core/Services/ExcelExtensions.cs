using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Drawing;

namespace Greg.Xrm.Command
{
	public static class ExcelExtensions
	{
		public static ExcelRange With(this ExcelRange range, Action<ExcelRange> action)
		{
			action(range);
			return range;
		}
		public static ExcelRange TextAlign(this ExcelRange range, ExcelHorizontalAlignment alignment)
		{
			range.Style.HorizontalAlignment = alignment;
			return range;
		}

		public static ExcelRange SetValue(this ExcelRange range, object? value)
		{
			range.Value = value;
			return range;
		}

		public static ExcelRange MergeCells(this ExcelRange range)
		{
			range.Merge = true;
			return range;
		}

		public static ExcelRange UnmergeCells(this ExcelRange range)
		{
			range.Merge = false;
			return range;
		}

		public static ExcelRange Title(this ExcelRange range)
		{
			range.Style.Font.SetFromFont("Calibri Light", 18);
			range.Style.Font.Color.SetColor(Color.FromArgb(68, 84, 106));
			return range;
		}

		public static ExcelRange Bold(this ExcelRange range)
		{
			range.Style.Font.Bold = true;
			return range;
		}

		public static ExcelRange Explanatory(this ExcelRange range)
		{
			range.Style.Font.SetFromFont("Calibri", 11, italic: true);
			range.Style.Font.Color.SetColor(Color.FromArgb(127, 127, 127));
			return range;
		}

		public static ExcelRange LightOrange60(this ExcelRange range)
		{
			range.Style.Fill.PatternType = ExcelFillStyle.Solid;
			range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(244, 176, 132));
			range.Style.Font.Color.SetColor(Color.Black);
			return range;
		}

		public static ExcelRange LightGreen60(this ExcelRange range)
		{
			range.Style.Fill.PatternType = ExcelFillStyle.Solid;
			range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(169, 208, 142));
			range.Style.Font.Color.SetColor(Color.Black);
			return range;
		}

		public static ExcelRange LightYellow60(this ExcelRange range)
		{
			range.Style.Fill.PatternType = ExcelFillStyle.Solid;
			range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 217, 102));
			range.Style.Font.Color.SetColor(Color.Black);
			return range;
		}

		public static ExcelRange LightBlue60(this ExcelRange range)
		{
			range.Style.Fill.PatternType = ExcelFillStyle.Solid;
			range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(155, 194, 230));
			range.Style.Font.Color.SetColor(Color.Black);
			return range;
		}

		public static ExcelRange DarkBlue60(this ExcelRange range)
		{
			range.Style.Fill.PatternType = ExcelFillStyle.Solid;
			range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(142, 169, 219));
			range.Style.Font.Color.SetColor(Color.Black);
			return range;
		}

		public static ExcelRange Input(this ExcelRange range)
		{
			range.Style.Fill.PatternType = ExcelFillStyle.Solid;
			range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 153));
			range.Style.Font.Color.SetColor(Color.FromArgb(63, 63, 118));
			range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(127, 127, 127));
			return range;
		}


		public static ExcelTable CreateTable(this ExcelWorksheet ws, string tableName, int fromRow, int fromCol, int toRow, int toCol, bool? autoFitColumns = true)
		{
			var table = ws.Tables.Add(ws.Cells[fromRow, fromCol, toRow, toCol], tableName);
			table.ShowHeader = true;
			table.ShowFilter = true;
			table.TableStyle = TableStyles.Medium2;

			if (autoFitColumns.GetValueOrDefault())
			{
				for (int i = fromCol; i <= toCol; i++)
				{
					ws.Column(i).AutoFit();
				}
			}
			


			return table;
		}
	}
}
