namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class RelationshipMetadata
    {
        public string Name { get; set; } = string.Empty;
        public RelationshipType Type { get; set; }
        public string? ChildEntity { get; set; }
        public string? ParentEntity { get; set; }
        public string? LookupField { get; set; }
        public string? LookupDisplayName { get; set; }
        public string? FirstEntity { get; set; }
        public string? SecondEntity { get; set; }
        public string? IntersectEntity { get; set; }
        public bool IsCustomRelationship { get; set; } = false;
    }

    public enum RelationshipType
    {
        OneToMany,
        ManyToMany
    }

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

    public class OptionSetOption
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class OptionSetMetadata
    {
        public string EntityName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string OptionSetName { get; set; } = string.Empty;
        public int OptionValue { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public OptionSetSourceType SourceType { get; set; }
    }

    public enum OptionSetSourceType
    {
        Global,
        Local,
        State
    }
}
