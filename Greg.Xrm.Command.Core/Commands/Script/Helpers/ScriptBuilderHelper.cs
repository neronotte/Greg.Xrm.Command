using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using Models = Greg.Xrm.Command.Commands.Script.Models;
using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.Helpers
{
    public static class ScriptBuilderHelper
    {
        public static void GenerateOptionSetCsv(List<Models.OptionSetMetadata> optionSets, string outputFilePath)
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
            csv.WriteField("OptionSetName");
            csv.WriteField("OptionValue");
            csv.WriteField("OptionLabel");
            csv.WriteField("SourceType");
            csv.NextRecord();
            foreach (var optionSet in optionSets.OrderBy(o => o.EntityName).ThenBy(o => o.FieldName).ThenBy(o => o.OptionValue))
            {
                csv.WriteField(optionSet.EntityName);
                csv.WriteField(optionSet.FieldName);
                csv.WriteField(optionSet.OptionSetName);
                csv.WriteField(optionSet.OptionValue);
                csv.WriteField(optionSet.OptionLabel);
                csv.WriteField(optionSet.SourceType.ToString());
                csv.NextRecord();
            }
        }

        private static void AppendCustomColumns(StringBuilder script, Models.EntityMetadata entity)
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
                    script.Append($"pacx column create --table \"{entity.SchemaName}\" --name \"{field.DisplayName}\" --schemaName \"{field.LogicalName}\" --type \"{field.FieldType}\"");
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
                        var options = string.Join(",", field.Options.Select(o => o.Label));
                        script.Append($" --options \"{options}\"");
                    }
                    script.AppendLine();
                }
            }
        }

        private static void AppendTableCreate(StringBuilder script, Models.EntityMetadata entity)
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

        private static void AppendRelationships(StringBuilder script, IEnumerable<Models.RelationshipMetadata> relationships, string customPrefix, HashSet<string> customEntityNames, HashSet<string> allEntityNames, string? entityNameFilter = null)
        {
            var customPrefixes = customPrefix.Split(',').Select(p => p.Trim()).ToList();
            var rels = relationships.OrderBy(r => r.Name);
            if (!string.IsNullOrEmpty(entityNameFilter))
            {
                rels = rels.Where(r =>
                    r.Type == Models.RelationshipType.OneToMany && (r.ParentEntity == entityNameFilter || r.ChildEntity == entityNameFilter) ||
                    r.Type == Models.RelationshipType.ManyToMany && (r.FirstEntity == entityNameFilter || r.SecondEntity == entityNameFilter)
                ).OrderBy(r => r.Name);
            }
            // Header and print n1
            script.AppendLine("# --- N:1 RELATIONSHIPS ---");
            foreach (var rel in rels.Where(r => r.Type == Models.RelationshipType.OneToMany))
            {
                bool isCustomLookup = rel.LookupField != null && customPrefixes.Any(p => rel.LookupField.StartsWith(p));
                if (isCustomLookup)
                {
                    script.AppendLine($"pacx rel create n1 --child \"{rel.ChildEntity}\" --parent \"{rel.ParentEntity}\" --relName \"{rel.Name}\" --lookupSchemaName \"{rel.LookupField}\"");
                }
            }
            // Header and print nn
            script.AppendLine("# --- N:N RELATIONSHIPS ---");
            foreach (var rel in rels.Where(r => r.Type == Models.RelationshipType.ManyToMany))
            {
                script.AppendLine($"pacx rel create nn --table1 \"{rel.FirstEntity}\" --table2 \"{rel.SecondEntity}\" --explicit --schemaName \"{rel.IntersectEntity}\"");
            }
        }

        public static string GeneratePacxScript(List<Models.EntityMetadata> entities, List<Models.RelationshipMetadata> relationships, string customPrefix)
        {
            var script = new StringBuilder();
            var customEntities = entities.Where(e => e.IsCustomEntity).OrderBy(e => e.SchemaName).ToList();
            var allEntityNames = new HashSet<string>(entities.Select(e => e.SchemaName));
            var customEntityNames = new HashSet<string>(customEntities.Select(e => e.SchemaName));
            // Intersect entities used in nn relationships
            var nnIntersectEntities = new HashSet<string>(relationships
                .Where(r => r.Type == Models.RelationshipType.ManyToMany && !string.IsNullOrEmpty(r.IntersectEntity))
                .Select(r => r.IntersectEntity!));
            script.AppendLine("# =====================================================");
            script.AppendLine("# DATAMODEL CREATION SCRIPT - COMPLETE VERSION");
            script.AppendLine("# =====================================================");
            script.AppendLine($"# Custom Prefix: {customPrefix}");
            script.AppendLine();
            script.AppendLine("# 1. CREATE ALL TABLES");
            foreach (var entity in customEntities)
            {
                // Skip intersect entities used in nn relationships
                if (nnIntersectEntities.Contains(entity.SchemaName))
                    continue;
                AppendTableCreate(script, entity);
            }
            script.AppendLine();
            script.AppendLine("# 2. CREATE ALL COLUMNS");
            foreach (var entity in entities.OrderBy(e => e.SchemaName))
            {
                AppendCustomColumns(script, entity);
            }
            script.AppendLine();
            script.AppendLine("# 3. CREATE ALL RELATIONSHIPS");
            AppendRelationships(script, relationships, customPrefix, customEntityNames, allEntityNames);
            return script.ToString();
        }

        public static string GeneratePacxScriptForTable(Models.EntityMetadata entity, string customPrefix, List<Models.RelationshipMetadata>? relationships = null)
        {
            var script = new StringBuilder();
            // If the table is an intersect entity used in an nn relationship, do not generate the table
            if (relationships != null)
            {
                var nnIntersectEntities = new HashSet<string>(
                    relationships.Where(r => r.Type == Models.RelationshipType.ManyToMany && !string.IsNullOrEmpty(r.IntersectEntity))
                                 .Select(r => r.IntersectEntity!)
                );
                if (nnIntersectEntities.Contains(entity.SchemaName))
                    return string.Empty;
            }
            script.AppendLine($"# PACX Script for table: {entity.SchemaName} with prefix: {customPrefix}");
            AppendTableCreate(script, entity);
            script.AppendLine();
            AppendCustomColumns(script, entity);
            // RELATIONSHIPS
            if (relationships != null)
            {
                script.AppendLine();
                script.AppendLine("# RELATIONSHIPS");
                var allEntityNames = new HashSet<string> { entity.SchemaName };
                var customEntityNames = new HashSet<string>();
                if (entity.IsCustomEntity)
                    customEntityNames.Add(entity.SchemaName);
                AppendRelationships(script, relationships, customPrefix, customEntityNames, allEntityNames, entity.SchemaName);
            }
            return script.ToString();
        }
    }
}
