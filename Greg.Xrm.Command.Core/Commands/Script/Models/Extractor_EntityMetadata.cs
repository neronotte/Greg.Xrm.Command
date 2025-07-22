using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class Extractor_EntityMetadata
    {
        public string SchemaName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PluralName { get; set; } = string.Empty;
        public bool IsCustomEntity { get; set; }
        public List<Extractor_FieldMetadata> Fields { get; set; } = new();
        public List<Extractor_RelationshipMetadata> Relationships { get; set; } = new();
        public List<Extractor_OptionSetMetadata> LocalOptionSets { get; set; } = new();
        public string? PrimaryFieldName { get; set; }
        public string? PrimaryFieldSchemaName { get; set; }
        public string? PrimaryFieldDescription { get; set; }
        public int? PrimaryFieldMaxLength { get; set; }
        public string? PrimaryFieldAutoNumberFormat { get; set; }
        public string? PrimaryFieldRequiredLevel { get; set; }
    }
}
