using Greg.Xrm.Command.Services.Output;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Greg.Xrm.Command.Commands.Script.MetadataExtractor;
using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.Service
{
    public class ScriptExtractionJob
    {
        private readonly IOutput output;
        private readonly ScriptMetadataExtractor metadataExtractor;
        private readonly List<string> prefixes;
        private readonly string outputDir;
        private readonly string pacxScriptName;
        private readonly string stateFieldsDefinitionName;
        private readonly bool exportStateFields;
        private readonly List<Extractor_EntityMetadata>? preloadedEntities;

        public ScriptExtractionJob(
            IOutput output,
            ScriptMetadataExtractor metadataExtractor,
            List<string> prefixes,
            string outputDir,
            string pacxScriptName,
            string stateFieldsDefinitionName,
            bool exportStateFields,
            List<Extractor_EntityMetadata>? preloadedEntities = null)
        {
            this.output = output;
            this.metadataExtractor = metadataExtractor;
            this.prefixes = prefixes;
            this.outputDir = outputDir;
            this.pacxScriptName = pacxScriptName;
            this.stateFieldsDefinitionName = stateFieldsDefinitionName;
            this.exportStateFields = exportStateFields;
            this.preloadedEntities = preloadedEntities;
        }

        public async Task<CommandResult> RunAsync()
        {
            List<Extractor_EntityMetadata> entities;
            if (preloadedEntities != null)
            {
                entities = preloadedEntities;
            }
            else
            {
                output.WriteLine("Step 1: Extracting entity metadata...");
                entities = await metadataExtractor.GetEntitiesByPrefixAsync(prefixes);
                output.WriteLine($"Entities found: {entities.Count}");
                foreach (var entity in entities)
                {
                    output.WriteLine($"  - {entity.SchemaName} ({entity.DisplayName}) - {entity.Fields.Count} fields");
                }
                output.WriteLine();
            }

            output.WriteLine("Step 2: Extracting relationship metadata...");
            var relationships = await metadataExtractor.GetRelationshipsAsync(prefixes, entities);
            output.WriteLine($"Relationships found: {relationships.Count}");
            foreach (var rel in relationships.OrderBy(r => r.Name))
            {
                if (rel.Type == Extractor_RelationshipType.OneToMany)
                    output.WriteLine($"  - {rel.Name}: {rel.ParentEntity} -> {rel.ChildEntity} ({rel.LookupField})");
                else
                    output.WriteLine($"  - {rel.Name}: {rel.FirstEntity} <-> {rel.SecondEntity}");
            }
            output.WriteLine();

            Directory.CreateDirectory(outputDir);

            output.WriteLine("Step 3: Generating PACX script...");
            var pacxScriptPath = Path.Combine(outputDir, pacxScriptName);
            var script = metadataExtractor.GeneratePacxScript(entities, relationships, prefixes);
            await File.WriteAllTextAsync(pacxScriptPath, script);
            output.WriteLine($"PACX script generated: {pacxScriptPath}");

            string? csvPath = null;
            if (exportStateFields)
            {
                output.WriteLine("Step 4: Generating State Field CSV...");
                csvPath = Path.Combine(outputDir, stateFieldsDefinitionName);
                var optionSets = await metadataExtractor.GetOptionSetsAsync(entities.Select(e => e.SchemaName).ToList());
                await metadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath);
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
