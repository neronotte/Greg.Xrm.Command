using Microsoft.Xrm.Sdk.Metadata;
using OfficeOpenXml;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public class DataverseColumn
	{


		public string? LogicalName { get; set; }
		public string? SchemaName { get; set; }
		public string? DisplayName { get; set; }
		public string? Description { get; set; }
		public string? PrimaryType { get; set; }
		public AttributeRequiredLevel? RequiredLevel { get; set; }
		public string? Type { get; set; }
		public string? Format { get; set; }
		public string? FormatName { get; set; }
		public int? MaxLength { get; set; }
		public string? AutoNumberFormat { get; set; }
		public string? Formula { get; set; }

		public string? LookupTargets { get; set; }
		public string? PicklistOptions { get; set; }

		public int? PicklistDefaultValue { get; set; }

		public bool? IsGlobalPicklist { get; set; }

		public object? MinValue { get; set; }
		public object? MaxValue { get; set; }
		public int? Precision { get; set; }
		public int? PrecisionSource { get; set; }

		public bool? IsAuditEnabled { get; set; }
		public bool? IsFlsEnabled { get; set; }
		public bool? IsFlsSecuredForCreate { get; set; }
		public bool? IsFlsSecuredForUpdate { get; set; }
		public bool? IsFlsSecuredForRead { get; set; }



		public static DataverseColumn CreateFrom(AttributeMetadata attribute, EntityMetadata entity)
		{
			var c = new DataverseColumn();

			c.LogicalName = attribute.LogicalName;
			c.SchemaName = attribute.SchemaName;
			c.DisplayName = attribute.DisplayName?.UserLocalizedLabel?.Label;
			c.Description = attribute.Description?.UserLocalizedLabel?.Label;
			c.PrimaryType = GetPrimary(attribute, entity);
			c.RequiredLevel = attribute.RequiredLevel?.Value;
			c.Type = attribute.AttributeType?.ToString();
			c.AutoNumberFormat = attribute.AutoNumberFormat;

			c.IsAuditEnabled = attribute.IsAuditEnabled?.Value;
			c.IsFlsEnabled = attribute.IsSecured;
			c.IsFlsSecuredForCreate = attribute.CanBeSecuredForCreate;
			c.IsFlsSecuredForUpdate = attribute.CanBeSecuredForUpdate;
			c.IsFlsSecuredForRead = attribute.CanBeSecuredForRead;


			if (attribute is StringAttributeMetadata a1) Fill(c, a1);
			if (attribute is MemoAttributeMetadata a2) Fill(c, a2);
			if (attribute is LookupAttributeMetadata a3) Fill(c, a3);
			if (attribute is DoubleAttributeMetadata a4) Fill(c, a4);
			if (attribute is DecimalAttributeMetadata a5) Fill(c, a5);
			if (attribute is IntegerAttributeMetadata a6) Fill(c, a6);
			if (attribute is PicklistAttributeMetadata a7) Fill(c, a7);
			if (attribute is MoneyAttributeMetadata a8) Fill(c, a8);
			if (attribute is StateAttributeMetadata a9) Fill(c, a9);
			if (attribute is StatusAttributeMetadata a10) Fill(c, a10);

			return c;
		}



		private static string? GetPrimary(AttributeMetadata attribute, EntityMetadata entityMetadata)
		{
			if (attribute.SchemaName == entityMetadata.PrimaryIdAttribute) return "ID";
			if (attribute.SchemaName == entityMetadata.PrimaryNameAttribute) return "Field";
			return string.Empty;
		}

		private static void Fill(DataverseColumn c, StringAttributeMetadata attribute)
		{
			c.Format = attribute.Format?.ToString();
			c.FormatName = attribute.FormatName?.Value;
			c.MaxLength = attribute.MaxLength;
			c.AutoNumberFormat = attribute.AutoNumberFormat;
			c.Formula = attribute.FormulaDefinition;
		}

		private static void Fill(DataverseColumn c, DoubleAttributeMetadata attribute)
		{
			c.MinValue = attribute.MinValue;
			c.MaxValue= attribute.MaxValue;
			c.Precision = attribute.Precision;
		}

		private static void Fill(DataverseColumn c, DecimalAttributeMetadata attribute)
		{
			c.Formula = attribute.FormulaDefinition;
			c.MinValue = attribute.MinValue;
			c.MaxValue = attribute.MaxValue;
			c.Precision = attribute.Precision;
		}

		private static void Fill(DataverseColumn c, IntegerAttributeMetadata attribute)
		{
			c.Format = attribute.Format?.ToString();
			c.Formula = attribute.FormulaDefinition;
			c.MinValue = attribute.MinValue;
			c.MaxValue = attribute.MaxValue;
		}

		private static void Fill(DataverseColumn c, MemoAttributeMetadata attribute)
		{
			c.Format = attribute.Format?.ToString();
			c.FormatName = attribute.FormatName?.Value;
			c.MaxLength = attribute.MaxLength;
			c.AutoNumberFormat = attribute.AutoNumberFormat;
		}

		private static void Fill(DataverseColumn c, LookupAttributeMetadata attribute)
		{
			c.Format = attribute.Format?.ToString();
			c.LookupTargets = string.Join(", ", attribute.Targets);
		}

		private static void Fill(DataverseColumn c, PicklistAttributeMetadata attribute)
		{
			var optionStringList = attribute.OptionSet.Options
				.Select(o => $"- {o.Value}: {o.Label?.UserLocalizedLabel?.Label}")
				.ToList();

			var optionString = "'" + string.Join(Environment.NewLine, optionStringList);

			c.PicklistOptions = optionString;
			c.PicklistDefaultValue = attribute.DefaultFormValue;
			c.IsGlobalPicklist = attribute.OptionSet.IsGlobal;
		}

		private static void Fill(DataverseColumn c, StateAttributeMetadata attribute)
		{
			var optionStringList = attribute.OptionSet.Options
				.Select(o => $"- {o.Value}: {o.Label?.UserLocalizedLabel?.Label}")
				.ToList();

			var optionString = "'" + string.Join(Environment.NewLine, optionStringList);

			c.PicklistOptions = optionString;
			c.PicklistDefaultValue = attribute.DefaultFormValue;
			c.IsGlobalPicklist = attribute.OptionSet.IsGlobal;
		}

		private static void Fill(DataverseColumn c, StatusAttributeMetadata attribute)
		{
			var optionStringList = attribute.OptionSet.Options
				.Select(o => $"- {o.Value}: {o.Label?.UserLocalizedLabel?.Label}")
				.ToList();

			var optionString = "'" + string.Join(Environment.NewLine, optionStringList);

			c.PicklistOptions = optionString;
			c.PicklistDefaultValue = attribute.DefaultFormValue;
			c.IsGlobalPicklist = attribute.OptionSet.IsGlobal;
		}

		private static void Fill(DataverseColumn c, MoneyAttributeMetadata attribute)
		{
			c.Formula = attribute.FormulaDefinition;
			c.MinValue = attribute.MinValue;
			c.MaxValue = attribute.MaxValue;
			c.Precision = attribute.Precision;
			c.PrecisionSource = attribute.PrecisionSource;
		}
	}
}
