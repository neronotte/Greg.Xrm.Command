using Microsoft.Xrm.Sdk.Metadata;
using Models = Greg.Xrm.Command.Commands.Script.Models;
using System.Linq;
using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.Helpers
{
    public static class RelationshipMetadataHelper
    {
        public static List<Models.RelationshipMetadata> ExtractRelationships(IEnumerable<EntityMetadata> entityMetadataList, List<string> prefixes, List<Models.EntityMetadata> includedEntities = null)
        {
            var includedNames = includedEntities?.Select(e => e.SchemaName).ToHashSet();
            var customNames = includedEntities?.Where(e => e.IsCustomEntity).Select(e => e.SchemaName).ToHashSet() ?? new HashSet<string>();
            var relationships = new List<Models.RelationshipMetadata>();
            var nnSet = new HashSet<string>();
            foreach (var entityMetadata in entityMetadataList)
            {
                foreach (var rel in entityMetadata.OneToManyRelationships ?? System.Array.Empty<OneToManyRelationshipMetadata>())
                {
                    bool isCustomParent = rel.ReferencedEntity != null && customNames.Contains(rel.ReferencedEntity);
                    bool isCustomChild = rel.ReferencingEntity != null && customNames.Contains(rel.ReferencingEntity);
                    if (isCustomParent || isCustomChild)
                    {
                        if ((includedNames == null || includedNames.Contains(rel.ReferencingEntity) || includedNames.Contains(rel.ReferencedEntity)) &&
                            prefixes.Any(pre => rel.ReferencingAttribute.StartsWith(pre)))
                        {
                            relationships.Add(new Models.RelationshipMetadata
                            {
                                Name = rel.SchemaName,
                                Type = Models.RelationshipType.OneToMany,
                                ParentEntity = rel.ReferencedEntity,
                                ChildEntity = rel.ReferencingEntity,
                                LookupField = rel.ReferencingAttribute
                            });
                        }
                    }
                }
                foreach (var rel in entityMetadata.ManyToManyRelationships ?? System.Array.Empty<ManyToManyRelationshipMetadata>())
                {
                    // Generate nn if at least one of the two entities is custom (IsCustomEntity)
                    bool isCustom1 = includedEntities?.Any(e => e.SchemaName == rel.Entity1LogicalName && e.IsCustomEntity) == true;
                    bool isCustom2 = includedEntities?.Any(e => e.SchemaName == rel.Entity2LogicalName && e.IsCustomEntity) == true;
                    if ((isCustom1 || isCustom2) && nnSet.Add(rel.SchemaName))
                    {
                        relationships.Add(new Models.RelationshipMetadata
                        {
                            Name = rel.SchemaName,
                            Type = Models.RelationshipType.ManyToMany,
                            FirstEntity = rel.Entity1LogicalName,
                            SecondEntity = rel.Entity2LogicalName,
                            IntersectEntity = rel.IntersectEntityName
                        });
                    }
                }
            }
            return relationships;
        }
    }
}
