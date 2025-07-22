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
            Func<List<string>, Task<List<Models.OptionSetMetadata>>> getOptionsets,
            string outputDir,
            string pacxScriptName,
            string stateFieldCsvName,
            Func<List<EntityMetadata>, Task<List<RelationshipMetadata>>> getRelationships,
            Func<List<Models.OptionSetMetadata>, string, Task> generateStateFieldCsv,
            Func<List<EntityMetadata>, List<RelationshipMetadata>, List<string>, string> generatePacxScript,
            bool exportStateFields = true,
            int step = 1)
        {
            output.WriteLine($"Step {step++}: Extracting entity metadata...");
            var entities = await getEntities();
            output.WriteLine($"Entities found: {entities.Count}");
            foreach (var entity in entities)
            {
                output.WriteLine($"  - {entity.SchemaName} ({entity.DisplayName}) - {entity.Fields.Count} fields");
            }
            output.WriteLine();

            output.WriteLine($"Step {step++}: Extracting relationship metadata...");
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

            Directory.CreateDirectory(outputDir);

            output.WriteLine($"Step {step++}: Generating PACX script...");
            var pacxScriptPath = Path.Combine(outputDir, pacxScriptName);
            var script = generatePacxScript(entities, relationships, prefixes);
            await File.WriteAllTextAsync(pacxScriptPath, script);
            output.WriteLine($"PACX script generated: {pacxScriptPath}");

            string csvPath = null;
            if (exportStateFields)
            {
                output.WriteLine($"Step {step++}: Generating State Field CSV...");
                csvPath = Path.Combine(outputDir, stateFieldCsvName);
                var options = await getOptionsets(prefixes);
                await generateStateFieldCsv(options, csvPath);
                output.WriteLine($"State Field CSV generated: {csvPath}");
                output.WriteLine();
            }

            output.WriteLine("Extraction completed successfully!");
            output.WriteLine("=================================");
            output.WriteLine($"Entities processed: {entities.Count}");
            output.WriteLine($"Relationships found: {relationships.Count}");
            if (exportStateFields)
                output.WriteLine($"State fields exported (statecode/statuscode): see {csvPath}");
            output.WriteLine();
            output.WriteLine("Output files:");
            output.WriteLine($"  - PACX Script: {pacxScriptPath}");
            if (exportStateFields)
                output.WriteLine($"  - State Field CSV: {csvPath}");
            return CommandResult.Success();
        }
    }
}
