using ClosedXML.Excel;

namespace Greg.Xrm.Command
{
	public static class ExcelExtensions
	{
		public static IXLCell With(this IXLCell range, Action<IXLCell> action)
		{
			action(range);
			return range;
		}

		public static IXLCells With(this IXLCells range, Action<IXLCells> action)
		{
			action(range);
			return range;
		}
		public static IXLCells TextAlign(this IXLCells range, XLAlignmentHorizontalValues alignment)
		{
			range.Style.Alignment.SetHorizontal(alignment);
			return range;
		}
		public static IXLRange TextAlign(this IXLRange range, XLAlignmentHorizontalValues alignment)
		{
			range.Style.Alignment.SetHorizontal(alignment);
			return range;
		}

		public static IXLCell TextAlign(this IXLCell range, XLAlignmentHorizontalValues alignment)
		{
			range.Style.Alignment.SetHorizontal(alignment);
			return range;
		}

		public static IXLCell SetValue(this IXLCell range, object? value)
		{
			range.SetValue(value);
			return range;
		}

		public static IXLCell Title(this IXLCell range)
		{
			range.Style.Font.SetFontName("Calibri Light");
			range.Style.Font.SetFontSize(18);
			range.Style.Font.SetFontColor(XLColor.FromArgb(68, 84, 106));
			return range;
		}
		public static IXLRange Title(this IXLRange range)
		{
			range.Style.Font.SetFontName("Calibri Light");
			range.Style.Font.SetFontSize(18);
			range.Style.Font.SetFontColor(XLColor.FromArgb(68, 84, 106));
			return range;
		}

		public static IXLCell Bold(this IXLCell range)
		{
			range.Style.Font.SetBold(true);
			return range;
		}

		public static IXLCell Explanatory(this IXLCell range)
		{
			range.Style.Font.SetFontName("Calibri");
			range.Style.Font.SetFontSize(11);
			range.Style.Font.SetItalic(true);
			range.Style.Font.SetFontColor(XLColor.FromArgb(127, 127, 127));
			return range;
		}

		public static IXLCell LightOrange60(this IXLCell range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(244, 176, 132));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLCell LightGreen60(this IXLCell range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(169, 208, 142));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLCell LightYellow60(this IXLCell range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 217, 102));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLCell LightBlue60(this IXLCell range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(155, 194, 230));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLCell DarkBlue60(this IXLCell range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(142, 169, 219));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLCell Input(this IXLCell range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 204, 153));
			range.Style.Font.SetFontColor(XLColor.FromArgb(63, 63, 118));
			range.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetTopBorderColor(XLColor.FromArgb(127, 127, 127));
			range.Style.Border.SetLeftBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetLeftBorderColor(XLColor.FromArgb(127, 127, 127));
			range.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetBottomBorderColor(XLColor.FromArgb(127, 127, 127));
			range.Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetRightBorderColor(XLColor.FromArgb(127, 127, 127));
			return range;
		}








		public static IXLRange Explanatory(this IXLRange range)
		{
			range.Style.Font.SetFontName("Calibri");
			range.Style.Font.SetFontSize(11);
			range.Style.Font.SetItalic(true);
			range.Style.Font.SetFontColor(XLColor.FromArgb(127, 127, 127));
			return range;
		}

		public static IXLRange LightOrange60(this IXLRange range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(244, 176, 132));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLRange LightGreen60(this IXLRange range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(169, 208, 142));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLRange LightYellow60(this IXLRange range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 217, 102));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLRange LightBlue60(this IXLRange range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(155, 194, 230));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLRange DarkBlue60(this IXLRange range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(142, 169, 219));
			range.Style.Font.SetFontColor(XLColor.FromArgb(0, 0, 0));
			return range;
		}

		public static IXLRange Input(this IXLRange range)
		{
			range.Style.Fill.PatternType = XLFillPatternValues.Solid;
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 204, 153));
			range.Style.Font.SetFontColor(XLColor.FromArgb(63, 63, 118));
			range.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetTopBorderColor(XLColor.FromArgb(127, 127, 127));
			range.Style.Border.SetLeftBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetLeftBorderColor(XLColor.FromArgb(127, 127, 127));
			range.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetBottomBorderColor(XLColor.FromArgb(127, 127, 127));
			range.Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
			range.Style.Border.SetRightBorderColor(XLColor.FromArgb(127, 127, 127));
			return range;
		}






		public static IXLTable CreateTable(this IXLWorksheet ws, string tableName, int fromRow, int fromCol, int toRow, int toCol, bool? autoFitColumns = true)
		{
			var table = ws.Range($"R{fromRow}C{fromCol}:R{toRow}C{toCol}").CreateTable(tableName);

			table.ShowHeaderRow = true;
			table.ShowAutoFilter = true;
			table.Theme = XLTableTheme.TableStyleMedium2;

			if (autoFitColumns.GetValueOrDefault())
			{
				for (int i = fromCol; i <= toCol; i++)
				{
					ws.Column(i).AdjustToContents();
				}
			}
			


			return table;
		}
	}
}
