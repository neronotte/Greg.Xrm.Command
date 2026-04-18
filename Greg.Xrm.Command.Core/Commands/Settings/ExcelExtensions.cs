using ClosedXML.Excel;
using Greg.Xrm.Command.Commands.Settings.Model;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Settings
{
	public static class ExcelExtensions
	{
		public static IXLCell SetFormat(this IXLCell cell, OptionSetValue? type, string? value)
		{
			if (type?.Value == (int)SettingDefinitionDataType.Boolean)
			{
				cell.AsRange().SetFormatBoolean(value);
				return cell;
			}
			if (type?.Value == (int)SettingDefinitionDataType.Number)
			{
				cell.AsRange().SetFormatNumber(value);
				return cell;
			}

			cell.SetValue(value);
			return cell;
		}


		public static IXLRange SetFormat(this IXLRange range, OptionSetValue? type, string? value)
		{
			if (type?.Value == (int)SettingDefinitionDataType.Boolean)
			{
				return range.SetFormatBoolean(value);
			}
			if (type?.Value == (int)SettingDefinitionDataType.Number)
			{
				return range.SetFormatNumber(value);
			}

			range.SetValue(value);
			return range;
		}

		public static IXLRange SetFormatBoolean(this IXLRange range, string? value)
		{
			range.SetValue(value?.ToString().ToUpperInvariant());
			range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			return range;
		}

		public static IXLRange SetFormatNumber(this IXLRange range, string? value)
		{
			range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
			range.Style.NumberFormat.SetFormat("#,##0.0");

			if (value != null && double.TryParse(value, out var number))
			{
				range.SetValue(number);
			}
			return range;
		}


		public static IXLRange ApplyValidation(this IXLRange range, OptionSetValue? value)
		{
			if (value == null) return range;
			if (value.Value == (int)SettingDefinitionDataType.Boolean)
			{
				return range.ApplyValidationBoolean();
			}
			if (value.Value == (int)SettingDefinitionDataType.Number)
			{
				return range.ApplyValidationNumber();
			}

			return range;
		}

		public static IXLRange ApplyValidationNumber(this IXLRange range)
		{
			var list = range.CreateDataValidation();
			list.Decimal.Between(-1000000000, 1000000000);
			list.IgnoreBlanks = true;
			list.ErrorMessage = "Invalid value, only numbers are supported!";
			list.ShowErrorMessage = true;
			list.ErrorStyle = XLErrorStyle.Stop;
			list.ErrorTitle = "Invalid value";
			return range;
		}

		public static IXLRange ApplyValidationBoolean(this IXLRange range)
		{
			var list = range.CreateDataValidation();
			list.List("\"TRUE,FALSE\"");
			list.IgnoreBlanks = true;
			list.ErrorMessage = "Invalid value, only 'true' and 'false' are supported!";
			list.ShowErrorMessage = true;
			list.ErrorStyle = XLErrorStyle.Stop;
			list.ErrorTitle = "Invalid value";
			return range;
		}

		public static IXLRange Unlocked(this IXLRange range, OptionSetValue? overridableLevel = null, params SettingDefinitionOverridableLevel[] validLevels)
		{
			var isLocked = overridableLevel != null && !validLevels.Contains((SettingDefinitionOverridableLevel)overridableLevel.Value);
			range.Style.Protection.SetLocked(isLocked);

			if (isLocked)
			{
				range.Style.Fill.PatternType = XLFillPatternValues.Solid;
				range.Style.Fill.SetBackgroundColor(XLColor.LightGray);
			}
			else
			{
				range.Input();
			}

			return range;
		}
	}
}
