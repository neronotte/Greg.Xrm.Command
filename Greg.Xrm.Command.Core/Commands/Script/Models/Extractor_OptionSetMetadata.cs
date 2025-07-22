namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class Extractor_OptionSetMetadata
    {
        public string EntityName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string OptionSetName { get; set; } = string.Empty;
        public int OptionValue { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public Extractor_OptionSetSourceType SourceType { get; set; }
    }
}
