using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class FieldMetadata
    {
        public string LogicalName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public string RequiredLevel { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public bool IsCustomField { get; set; }
        public bool IsLookup { get; set; }
        public List<OptionSetOption> Options { get; set; } = new();
        public string? GlobalOptionSetName { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public int? Precision { get; set; }
        public int? PrecisionSource { get; set; }
        public string? DateTimeBehavior { get; set; }
        public string? DateTimeFormat { get; set; }
        public string? TrueLabel { get; set; }
        public string? FalseLabel { get; set; }
        public string? AutoNumberFormat { get; set; }
        public string? IntegerFormat { get; set; }
    }
}
