using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.MetadataExtractor;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.Service
{
    public class ScriptExtractionService : IScriptExtractionService
    {
        private readonly IOutput output;
        private readonly ScriptMetadataExtractor metadataExtractor;

        public ScriptExtractionService(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
            this.output = output;
            metadataExtractor = new ScriptMetadataExtractor(organizationServiceRepository);
        }

        public async Task<CommandResult> ExtractAllAsync(ScriptAllCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? new List<string>();
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            var exportStateFields = command.WithStateFieldsDefinition;
            var pacxScriptName = command.PacxScriptName;
            var stateFieldsDefinitionName = command.StateFieldsDefinitionName;

            return await new ScriptExtractionJob(
                output,
                metadataExtractor,
                prefixes,
                outputDir,
                pacxScriptName,
                stateFieldsDefinitionName,
                exportStateFields
            ).RunAsync();
        }

        public async Task<CommandResult> ExtractSolutionAsync(ScriptSolutionCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? new List<string>();
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            var exportStateFields = command.WithStateFieldsDefinition;
            var pacxScriptName = command.PacxScriptName;
            var stateFieldsDefinitionName = command.StateFieldsDefinitionName;
            var solutionNames = command.SolutionNames?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();
            if (solutionNames.Count == 0)
            {
                output.WriteLine("No solution names provided.", ConsoleColor.Red);
                return CommandResult.Fail("No solution names provided.");
            }
            output.WriteLine("Step 1: Extracting solution entities...");
            var allEntities = new List<Extractor_EntityMetadata>();
            foreach (var solutionName in solutionNames)
            {
                var entities = await metadataExtractor.GetEntitiesBySolutionAsync(solutionName, prefixes);
                allEntities.AddRange(entities);
            }
            allEntities = allEntities.GroupBy(e => e.SchemaName).Select(g => g.First()).ToList();
            return await new ScriptExtractionJob(
                output,
                metadataExtractor,
                prefixes,
                outputDir,
                pacxScriptName,
                stateFieldsDefinitionName,
                exportStateFields,
                allEntities
            ).RunAsync();
        }

        public async Task<CommandResult> ExtractTableAsync(ScriptTableCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? new List<string>();
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            var exportStateFields = command.WithStateFieldsDefinition;
            var pacxScriptName = command.PacxScriptName;
            var stateFieldsDefinitionName = command.StateFieldsDefinitionName;
            output.WriteLine("Step 1: Extracting table metadata...");
            var entity = await metadataExtractor.GetTableAsync(command.TableName, prefixes);
            if (entity == null)
            {
                output.WriteLine($"Table not found: {command.TableName}", ConsoleColor.Red);
                return CommandResult.Fail($"Table not found: {command.TableName}");
            }
            return await new ScriptExtractionJob(
                output,
                metadataExtractor,
                prefixes,
                outputDir,
                pacxScriptName,
                stateFieldsDefinitionName,
                exportStateFields,
                new List<Extractor_EntityMetadata> { entity }
            ).RunAsync();
        }
    }
}
