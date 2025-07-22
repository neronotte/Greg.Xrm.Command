using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using Models = Greg.Xrm.Command.Commands.Script.Models;
using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.Service
{
    public class ScriptBuilder
    {
        public void GenerateOptionSetCsv(List<Models.OptionSetMetadata> optionSets, string outputFilePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                Quote = '"',
                ShouldQuote = (args) => true
            };
            using var writer = new StreamWriter(outputFilePath);
            using var csv = new CsvWriter(writer, config);
            csv.WriteField("EntityName");
            csv.WriteField("FieldName");
            csv.WriteField("OptionValue");
            csv.WriteField("OptionLabel");
            csv.WriteField("SourceType");
            csv.NextRecord();
            var stateFields = optionSets
                    .Where(f => f.FieldName == "statecode" || f.FieldName == "statuscode")
                    .Select(f => f)
                    .OrderBy(f => f.EntityName)
                .ToList();
            foreach (var item in stateFields)
            {
                csv.WriteField(item.EntityName);
                csv.WriteField(item.FieldName);
                csv.WriteField(item.OptionValue);
                csv.WriteField(item.OptionLabel);
                csv.WriteField(item.FieldName == "statecode" ? "State" : "Status");
                csv.NextRecord();
            }
        }

        // Tutti i metodi privati statici diventano privati di istanza
        private void AppendCustomColumns(StringBuilder script, Models.EntityMetadata entity)
        {
            var lookupNames = entity.Fields.Where(f => f.IsCustomField && f.IsLookup).Select(f => f.LogicalName).ToHashSet();
            var customFields = entity.Fields
                .Where(f => f.IsCustomField && !f.IsLookup)
                .Where(f => !(f.LogicalName.EndsWith("name") && lookupNames.Contains(f.LogicalName.Substring(0, f.LogicalName.Length - 4))))
                .OrderBy(f => f.LogicalName)
                .ToList();
            if (customFields.Any())
            {
                script.AppendLine();
                script.AppendLine($"# ===== {entity.SchemaName.ToUpper()} COLUMNS =====");
                foreach (var field in customFields)
                {
                    var prefix = field.FieldType == "Uniqueidentifier" ? "# " : string.Empty;
                    script.Append($"{prefix}pacx column create --table \"{entity.SchemaName}\" --name \"{field.DisplayName}\" --schemaName \"{field.LogicalName}\" --type \"{field.FieldType}\"");
                    if (field.MaxLength.HasValue)
                        script.Append($" --len {field.MaxLength.Value}");
                    if (!string.IsNullOrEmpty(field.Format))
                        script.Append($" --stringFormat \"{field.Format}\"");
                    if (!string.IsNullOrEmpty(field.AutoNumberFormat))
                        script.Append($" --autoNumber \"{field.AutoNumberFormat}\"");
                    if (!string.IsNullOrEmpty(field.IntegerFormat))
                        script.Append($" --intFormat \"{field.IntegerFormat}\"");
                    if (!string.IsNullOrEmpty(field.RequiredLevel))
                        script.Append($" --requiredLevel \"{field.RequiredLevel}\"");
                    if (field.MinValue.HasValue)
                        script.Append($" --min {field.MinValue.Value}");
                    if (field.MaxValue.HasValue)
                        script.Append($" --max {field.MaxValue.Value}");
                    if (field.Precision.HasValue)
                        script.Append($" --precision {field.Precision.Value}");
                    if (field.PrecisionSource.HasValue)
                        script.Append($" --precisionSource {field.PrecisionSource.Value}");
                    if (!string.IsNullOrEmpty(field.DateTimeBehavior))
                        script.Append($" --dateTimeBehavior \"{field.DateTimeBehavior}\"");
                    if (!string.IsNullOrEmpty(field.DateTimeFormat))
                        script.Append($" --dateTimeFormat \"{field.DateTimeFormat}\"");
                    if (!string.IsNullOrEmpty(field.TrueLabel))
                        script.Append($" --trueLabel \"{field.TrueLabel}\"");
                    if (!string.IsNullOrEmpty(field.FalseLabel))
                        script.Append($" --falseLabel \"{field.FalseLabel}\"");
                    if (!string.IsNullOrEmpty(field.GlobalOptionSetName))
                        script.Append($" --globalOptionSetName \"{field.GlobalOptionSetName}\"");
                    if (field.Options != null && field.Options.Any())
                    {
                        var options = string.Join(",", field.Options.Select(o => $"{o.Label}:{o.Value}"));
                        script.Append($" --options \"{options}\"");
                    }
                    script.AppendLine();
                }
            }
        }

        private void AppendTableCreate(StringBuilder script, Models.EntityMetadata entity)
        {
            script.Append($"pacx table create --name \"{entity.DisplayName}\" --plural \"{entity.PluralName}\" --schemaName \"{entity.SchemaName}\"");
            if (!string.IsNullOrEmpty(entity.PrimaryFieldName))
                script.Append($" --primaryAttributeName \"{entity.PrimaryFieldName}\"");
            if (!string.IsNullOrEmpty(entity.PrimaryFieldSchemaName))
                script.Append($" --primaryAttributeSchemaName \"{entity.PrimaryFieldSchemaName}\"");
            if (!string.IsNullOrEmpty(entity.PrimaryFieldDescription))
                script.Append($" --primaryAttributeDescription \"{entity.PrimaryFieldDescription}\"");
            if (entity.PrimaryFieldMaxLength.HasValue)
                script.Append($" --primaryAttributeMaxLength {entity.PrimaryFieldMaxLength.Value}");
            if (!string.IsNullOrEmpty(entity.PrimaryFieldAutoNumberFormat))
                script.Append($" --primaryAttributeAutoNumberFormat \"{entity.PrimaryFieldAutoNumberFormat}\"");
            if (!string.IsNullOrEmpty(entity.PrimaryFieldRequiredLevel))
                script.Append($" --primaryAttributeRequiredLevel \"{entity.PrimaryFieldRequiredLevel}\"");
            script.AppendLine();
        }

        private void AppendRelationships(StringBuilder script, IEnumerable<Models.RelationshipMetadata> relationships, List<string> customPrefixes, HashSet<string> customEntityNames, HashSet<string> allEntityNames, string? entityNameFilter = null)
        {
            var rels = relationships.Where(r => r.IsCustomRelationship).OrderBy(r => r.Name);
            if (!string.IsNullOrEmpty(entityNameFilter))
            {
                rels = rels.Where(r =>
                    r.Type == Models.RelationshipType.OneToMany && (r.ParentEntity == entityNameFilter || r.ChildEntity == entityNameFilter) ||
                    r.Type == Models.RelationshipType.ManyToMany && (r.FirstEntity == entityNameFilter || r.SecondEntity == entityNameFilter)
                ).OrderBy(r => r.Name);
            }
            // Header and print n1
            script.AppendLine("# --- N:1 RELATIONSHIPS ---");
            foreach (var rel in rels.DistinctBy(r => r.Name).Where(r => r.Type == Models.RelationshipType.OneToMany))
            {
                bool isCustomLookup = rel.LookupField != null && customPrefixes.Any(p => rel.LookupField.StartsWith(p));
                if (isCustomLookup)
                {
                    script.AppendLine($"pacx rel create n1 --child \"{rel.ChildEntity}\" --parent \"{rel.ParentEntity}\" --relName \"{rel.Name}\" --lookupSchemaName \"{rel.LookupField}\"");
                }
            }
            // Header and print nn
            script.AppendLine("# --- N:N RELATIONSHIPS ---");
            foreach (var rel in rels.DistinctBy(r => r.IntersectEntity).Where(r => r.Type == Models.RelationshipType.ManyToMany))
            {
                script.AppendLine($"pacx rel create nn --table1 \"{rel.FirstEntity}\" --table2 \"{rel.SecondEntity}\" --explicit --schemaName \"{rel.IntersectEntity}\"");
            }
        }

        private void AppendStandardTableCreate(StringBuilder commentedSection, Models.EntityMetadata entity)
        {
            var sb = new StringBuilder();
            AppendTableCreate(sb, entity);
            foreach (var line in sb.ToString().Split('\n'))
                commentedSection.AppendLine("# " + line.TrimEnd());
        }

        private void AppendStandardColumns(StringBuilder commentedSection, Models.EntityMetadata entity)
        {
            var lookupNames = entity.Fields.Where(f => f.IsLookup).Select(f => f.LogicalName).ToHashSet();
            var standardFields = entity.Fields
                .Where(f => !f.IsCustomField && !f.IsLookup)
                .Where(f => !(f.LogicalName.EndsWith("name") && lookupNames.Contains(f.LogicalName.Substring(0, f.LogicalName.Length - 4))))
                .OrderBy(f => f.LogicalName)
                .ToList();
            if (standardFields.Any())
            {
                commentedSection.AppendLine();
                commentedSection.AppendLine($"# ===== {entity.SchemaName.ToUpper()} STANDARD COLUMNS =====");
                foreach (var field in standardFields)
                {
                    var sb = new StringBuilder();
                    sb.Append($"pacx column create --table \"{entity.SchemaName}\" --name \"{field.DisplayName}\" --schemaName \"{field.LogicalName}\" --type \"{field.FieldType}\"");
                    if (field.MaxLength.HasValue)
                        sb.Append($" --len {field.MaxLength.Value}");
                    if (!string.IsNullOrEmpty(field.Format))
                        sb.Append($" --stringFormat \"{field.Format}\"");
                    if (!string.IsNullOrEmpty(field.AutoNumberFormat))
                        sb.Append($" --autoNumber \"{field.AutoNumberFormat}\"");
                    if (!string.IsNullOrEmpty(field.IntegerFormat))
                        sb.Append($" --intFormat \"{field.IntegerFormat}\"");
                    if (!string.IsNullOrEmpty(field.RequiredLevel))
                        sb.Append($" --requiredLevel \"{field.RequiredLevel}\"");
                    if (field.MinValue.HasValue)
                        sb.Append($" --min {field.MinValue.Value}");
                    if (field.MaxValue.HasValue)
                        sb.Append($" --max {field.MaxValue.Value}");
                    if (field.Precision.HasValue)
                        sb.Append($" --precision {field.Precision.Value}");
                    if (field.PrecisionSource.HasValue)
                        sb.Append($" --precisionSource {field.PrecisionSource.Value}");
                    if (!string.IsNullOrEmpty(field.DateTimeBehavior))
                        sb.Append($" --dateTimeBehavior \"{field.DateTimeBehavior}\"");
                    if (!string.IsNullOrEmpty(field.DateTimeFormat))
                        sb.Append($" --dateTimeFormat \"{field.DateTimeFormat}\"");
                    if (!string.IsNullOrEmpty(field.TrueLabel))
                        sb.Append($" --trueLabel \"{field.TrueLabel}\"");
                    if (!string.IsNullOrEmpty(field.FalseLabel))
                        sb.Append($" --falseLabel \"{field.FalseLabel}\"");
                    if (!string.IsNullOrEmpty(field.GlobalOptionSetName))
                        sb.Append($" --globalOptionSetName \"{field.GlobalOptionSetName}\"");
                    if (field.Options != null && field.Options.Any())
                    {
                        var options = string.Join(",", field.Options.Select(o => $"{o.Label}:{o.Value}"));
                        sb.Append($" --options \"{options}\"");
                    }
                    commentedSection.AppendLine("# " + sb.ToString());
                }
            }
        }

        private void AppendStandardRelationships(StringBuilder commentedSection, IEnumerable<Models.RelationshipMetadata> relationships, List<string> customPrefixes, HashSet<string> customEntityNames, HashSet<string> allEntityNames, string? entityNameFilter = null)
        {
            var rels = relationships.Where(r => !r.IsCustomRelationship).OrderBy(r => r.Name);
            if (!string.IsNullOrEmpty(entityNameFilter))
            {
                rels = rels.Where(r =>
                    r.Type == Models.RelationshipType.OneToMany && (r.ParentEntity == entityNameFilter || r.ChildEntity == entityNameFilter) ||
                    r.Type == Models.RelationshipType.ManyToMany && (r.FirstEntity == entityNameFilter || r.SecondEntity == entityNameFilter)
                ).OrderBy(r => r.Name);
            }
            // Header and print n1
            commentedSection.AppendLine();
            commentedSection.AppendLine("# --- N:1 RELATIONSHIPS (STANDARD) ---");
            foreach (var rel in rels.DistinctBy(r => r.Name).Where(r => r.Type == Models.RelationshipType.OneToMany))
            {
                bool isCustomLookup = rel.LookupField != null && customPrefixes.Any(p => rel.LookupField.StartsWith(p));
                if (!isCustomLookup)
                {
                    commentedSection.AppendLine($"# pacx rel create n1 --child \"{rel.ChildEntity}\" --parent \"{rel.ParentEntity}\" --relName \"{rel.Name}\" --lookupSchemaName \"{rel.LookupField}\"");
                }
            }
            // Header and print nn
            commentedSection.AppendLine("# --- N:N RELATIONSHIPS (STANDARD) ---");
            foreach (var rel in rels.DistinctBy(r => r.IntersectEntity).Where(r => r.Type == Models.RelationshipType.ManyToMany && !customPrefixes.Any(pre => r.FirstEntity.StartsWith(pre)) && !customPrefixes.Any(pre => r.SecondEntity.StartsWith(pre))))
            {
                commentedSection.AppendLine($"# pacx rel create nn --table1 \"{rel.FirstEntity}\" --table2 \"{rel.SecondEntity}\" --explicit --schemaName \"{rel.IntersectEntity}\"");
            }
        }

        public string GeneratePacxScript(List<Models.EntityMetadata> entities, List<Models.RelationshipMetadata> relationships, List<string> prefixes)
        {
            var script = new StringBuilder();
            var commentedSection = new StringBuilder();
            var customEntities = entities.Where(e => e.IsCustomEntity).OrderBy(e => e.SchemaName).ToList();
            var standardEntities = entities.Where(e => !e.IsCustomEntity).OrderBy(e => e.SchemaName).ToList();
            var allEntityNames = new HashSet<string>(entities.Select(e => e.SchemaName));
            var customEntityNames = new HashSet<string>(customEntities.Select(e => e.SchemaName));
            var standardEntityNames = new HashSet<string>(standardEntities.Select(e => e.SchemaName));
            // Intersect entities used in nn relationships
            var nnIntersectEntities = new HashSet<string>(relationships
                .Where(r => r.Type == Models.RelationshipType.ManyToMany && !string.IsNullOrEmpty(r.IntersectEntity))
                .Select(r => r.IntersectEntity!));
            script.AppendLine("# =====================================================");
            script.AppendLine("# DATAMODEL CREATION SCRIPT");
            script.AppendLine("# =====================================================");
            script.AppendLine($"# Custom Prefix: {string.Join(", ", prefixes)}");
            script.AppendLine();
            script.AppendLine("# 1. CREATE ALL TABLES");
            foreach (var entity in customEntities)
            {
                // Skip intersect entities used in nn relationships
                if (nnIntersectEntities.Contains(entity.SchemaName))
                    continue;
                AppendTableCreate(script, entity);
            }
            // Commented: standard entities
            foreach (var entity in standardEntities)
            {
                if (nnIntersectEntities.Contains(entity.SchemaName))
                    continue;
                AppendStandardTableCreate(commentedSection, entity);
            }
            script.AppendLine();
            script.AppendLine("# 2. CREATE ALL COLUMNS");
            foreach (var entity in entities.OrderBy(e => e.SchemaName))
            {
                if (nnIntersectEntities.Contains(entity.SchemaName))
                    continue;
                AppendCustomColumns(script, entity);
                AppendStandardColumns(commentedSection, entity);
            }
            script.AppendLine();
            script.AppendLine("# 3. CREATE ALL RELATIONSHIPS");
            AppendRelationships(script, relationships, prefixes, customEntityNames, allEntityNames);
            AppendStandardRelationships(commentedSection, relationships, prefixes, customEntityNames, allEntityNames);

            // Add commented section at the end for strandard elements
            if (commentedSection.Length > 0)
            {
                script.AppendLine();
                script.AppendLine("# ===================== STANDARD ENTITIES/RELATIONSHIPS =====================");
                script.Append(commentedSection.ToString());
            }
            return script.ToString();
        }
    }
}
