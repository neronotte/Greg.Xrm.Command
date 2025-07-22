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
}
