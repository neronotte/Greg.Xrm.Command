using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class Extractor_RelationshipMetadata
    {
        public string Name { get; set; } = string.Empty;
        public Extractor_RelationshipType Type { get; set; }
        public string? ChildEntity { get; set; }
        public string? ParentEntity { get; set; }
        public string? LookupField { get; set; }
        public string? LookupDisplayName { get; set; }
        public string? FirstEntity { get; set; }
        public string? SecondEntity { get; set; }
        public string? IntersectEntity { get; set; }
        public bool IsCustomRelationship { get; set; } = false;

        public static List<Extractor_RelationshipMetadata> ExtractRelationships(IEnumerable<EntityMetadata> entityMetadataList, List<string> prefixes, List<Extractor_EntityMetadata>? includedEntities)
        {
            var includedNames = includedEntities?.Select(e => e.SchemaName).ToHashSet();
            var customNames = includedEntities?.Where(e => e.IsCustomEntity).Select(e => e.SchemaName).ToHashSet() ?? new HashSet<string>();
            var singleTable = includedEntities?.Count == 1;
            var relationships = new List<Extractor_RelationshipMetadata>();
            var nnSet = new HashSet<string>();

            foreach (var entityMetadata in entityMetadataList)
            {
                foreach (var rel in entityMetadata.OneToManyRelationships ?? [])
                {
                    bool isCustomParent = rel.ReferencedEntity != null && customNames.Contains(rel.ReferencedEntity);
                    bool isCustomChild = rel.ReferencingEntity != null && customNames.Contains(rel.ReferencingEntity);
                    if (includedNames != null && includedNames.Count != 0 &&
                       (singleTable || !includedNames.Contains(rel.ReferencingEntity!) || !includedNames.Contains(rel.ReferencedEntity!)) &&
                       (!singleTable || !includedNames.Contains(rel.ReferencingEntity!) && !includedNames.Contains(rel.ReferencedEntity!)))
                    {
                        continue;
                    }
                    relationships.Add(new Extractor_RelationshipMetadata
                    {
                        Name = rel.SchemaName,
                        Type = Extractor_RelationshipType.OneToMany,
                        ParentEntity = rel.ReferencedEntity,
                        ChildEntity = rel.ReferencingEntity,
                        LookupField = rel.ReferencingAttribute,
                        IsCustomRelationship = (isCustomParent || isCustomChild) &&
                            prefixes.Any(pre => rel.ReferencingAttribute.StartsWith(pre))
                    });
                }

                foreach (var rel in entityMetadata.ManyToManyRelationships ?? [])
                {
                    bool isCustom1 = includedEntities?.Any(e => e.SchemaName == rel.Entity1LogicalName && e.IsCustomEntity) == true;
                    bool isCustom2 = includedEntities?.Any(e => e.SchemaName == rel.Entity2LogicalName && e.IsCustomEntity) == true;
                    if (includedNames != null && includedNames.Count != 0 &&
                        (singleTable || !includedNames.Contains(rel.Entity1LogicalName) || !includedNames.Contains(rel.Entity2LogicalName)) &&
                        (!singleTable || !includedNames.Contains(rel.Entity1LogicalName) && !includedNames.Contains(rel.Entity2LogicalName)))
                    {
                        continue;
                    }
                    relationships.Add(new Extractor_RelationshipMetadata
                    {
                        Name = rel.SchemaName,
                        Type = Extractor_RelationshipType.ManyToMany,
                        FirstEntity = rel.Entity1LogicalName,
                        SecondEntity = rel.Entity2LogicalName,
                        IntersectEntity = rel.IntersectEntityName,
                        IsCustomRelationship = (isCustom1 || isCustom2) && nnSet.Add(rel.SchemaName)
                    });
                }
            }
            return relationships;
        }
    }
}
