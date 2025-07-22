using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.Helpers
{
    public static class ScriptExtractionHelper
    {
        public static async Task<CommandResult> ExecuteScriptExtractionAsync(
            IOutput output,
            ScriptMetadataExtractor metadataExtractor,
            List<string> prefixes,
            Func<Task<List<EntityMetadata>>> getEntities,
            string outputDir,
            string pacxScriptName,
            string optionSetCsvName,
            Func<List<EntityMetadata>, Task<List<RelationshipMetadata>>> getRelationships,
            Func<List<string>, Task<List<OptionSetMetadata>>> getOptionSets,
            Func<List<EntityMetadata>, List<RelationshipMetadata>, string, string> generatePacxScript,
            bool exportOptionSets = true)
        {
            output.WriteLine("Step 1: Extracting entity metadata...");
            var entities = await getEntities();
            output.WriteLine($"Entities found: {entities.Count}");
            foreach (var entity in entities)
            {
                output.WriteLine($"  - {entity.SchemaName} ({entity.DisplayName}) - {entity.Fields.Count} fields");
            }
            output.WriteLine();

            output.WriteLine("Step 2: Extracting relationship metadata...");
            var relationships = await getRelationships(entities);
            output.WriteLine($"Relationships found: {relationships.Count}");
            foreach (var rel in relationships.OrderBy(r => r.Name))
            {
                if (rel.Type == RelationshipType.OneToMany)
                    output.WriteLine($"  - {rel.Name}: {rel.ParentEntity} -> {rel.ChildEntity} ({rel.LookupField})");
                else
                    output.WriteLine($"  - {rel.Name}: {rel.FirstEntity} <-> {rel.SecondEntity}");
            }
            output.WriteLine();

            List<OptionSetMetadata> optionSets = null;
            if (exportOptionSets)
            {
                output.WriteLine("Step 3: Extracting OptionSet metadata...");
                optionSets = await getOptionSets(prefixes);
                output.WriteLine($"OptionSet options found: {optionSets.Count}");
                output.WriteLine();
            }

            Directory.CreateDirectory(outputDir);

            output.WriteLine("Step 4: Generating PACX script...");
            var pacxScriptPath = Path.Combine(outputDir, pacxScriptName);
            var script = generatePacxScript(entities, relationships, prefixes.FirstOrDefault() ?? "");
            await File.WriteAllTextAsync(pacxScriptPath, script);
            output.WriteLine($"PACX script generated: {pacxScriptPath}");

            string csvPath = null;
            if (exportOptionSets && optionSets != null)
            {
                output.WriteLine("Step 5: Generating OptionSet CSV...");
                csvPath = Path.Combine(outputDir, optionSetCsvName);
                metadataExtractor.GenerateOptionSetCsv(optionSets, csvPath);
                output.WriteLine($"OptionSet CSV generated: {csvPath}");
                output.WriteLine();
            }

            output.WriteLine("Extraction completed successfully!");
            output.WriteLine("=================================");
            output.WriteLine($"Entities processed: {entities.Count}");
            output.WriteLine($"Relationships found: {relationships.Count}");
            if (exportOptionSets && optionSets != null)
                output.WriteLine($"OptionSet options exported: {optionSets.Count}");
            output.WriteLine();
            output.WriteLine("Output files:");
            output.WriteLine($"  - PACX Script: {pacxScriptPath}");
            if (exportOptionSets && optionSets != null)
                output.WriteLine($"  - OptionSet CSV: {csvPath}");
            return CommandResult.Success();
        }
    }
}
