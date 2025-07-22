using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class EntityMetadata
    {
        public string SchemaName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PluralName { get; set; } = string.Empty;
        public bool IsCustomEntity { get; set; }
        public List<FieldMetadata> Fields { get; set; } = new();
        public List<RelationshipMetadata> Relationships { get; set; } = new();
        public List<OptionSetMetadata> LocalOptionSets { get; set; } = new();
        public string? PrimaryFieldName { get; set; }
        public string? PrimaryFieldSchemaName { get; set; }
        public string? PrimaryFieldDescription { get; set; }
        public int? PrimaryFieldMaxLength { get; set; }
        public string? PrimaryFieldAutoNumberFormat { get; set; }
        public string? PrimaryFieldRequiredLevel { get; set; }
    }
}
