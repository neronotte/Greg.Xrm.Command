using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using System.Collections.Generic;
using Greg.Xrm.Command.Commands.Script.Helpers;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptAllCommandExecutor : ICommandExecutor<ScriptAllCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly ScriptMetadataExtractor metadataExtractor;
        public ScriptAllCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
            this.output = output;
            this.organizationServiceRepository = organizationServiceRepository;
            this.metadataExtractor = new ScriptMetadataExtractor(organizationServiceRepository);
        }

        public async Task<CommandResult> ExecuteAsync(ScriptAllCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? new List<string>();
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            return await ScriptExtractionHelper.ExecuteScriptExtractionAsync(
                output,
                metadataExtractor,
                prefixes,
                () => metadataExtractor.GetEntitiesByPrefixAsync(prefixes),
                (prefixes) => metadataExtractor.GetOptionSetsAsync(),
                outputDir,
                command.PacxScriptName,
                command.StateFieldsDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithStateFieldsDefinition ? (Func<List<Models.OptionSetMetadata>, string, Task>)((optionSets, csvPath) => metadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath)) : ((optionSets, csvPath) => Task.CompletedTask),
                (entities, relationships, prefix) => metadataExtractor.GeneratePacxScript(entities, relationships, prefix),
                command.WithStateFieldsDefinition
            );
        }
    }

    public class ScriptSolutionCommandExecutor : ICommandExecutor<ScriptSolutionCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly ScriptMetadataExtractor metadataExtractor;
        public ScriptSolutionCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
            this.output = output;
            this.organizationServiceRepository = organizationServiceRepository;
            this.metadataExtractor = new ScriptMetadataExtractor(organizationServiceRepository);
        }
        public async Task<CommandResult> ExecuteAsync(ScriptSolutionCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? new List<string>();
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            var solutionNames = command.SolutionNames?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();
            if (solutionNames.Count == 0)
            {
                output.WriteLine("No solution names provided.", ConsoleColor.Red);
                return CommandResult.Fail("No solution names provided.");
            }
            output.WriteLine("Step 1: Extracting solution entities...");
            var allEntities = new List<Models.EntityMetadata>();
            foreach (var solutionName in solutionNames)
            {
                var entities = await metadataExtractor.GetEntitiesBySolutionAsync(solutionName, prefixes);
                allEntities.AddRange(entities);
            }
            // Remove duplicates by SchemaName
            allEntities = allEntities.GroupBy(e => e.SchemaName).Select(g => g.First()).ToList();
            return await ScriptExtractionHelper.ExecuteScriptExtractionAsync(
                output,
                metadataExtractor,
                prefixes,
                () => Task.FromResult(allEntities),
                (prefixes) => metadataExtractor.GetOptionSetsAsync(allEntities.Select(e => e.SchemaName).ToList()),
                outputDir,
                command.PacxScriptName,
                command.StateFieldsDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithStateFieldsDefinition ? (Func<List<Models.OptionSetMetadata>, string, Task>)((optionSets, csvPath) => metadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath)) : ((optionSets, csvPath) => Task.CompletedTask),
                (entities, relationships, prefix) => metadataExtractor.GeneratePacxScript(entities, relationships, prefix),
                command.WithStateFieldsDefinition,
                2
            );
        }
    }

    public class ScriptTableCommandExecutor : ICommandExecutor<ScriptTableCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly ScriptMetadataExtractor metadataExtractor;
        public ScriptTableCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
        {
            this.output = output;
            this.organizationServiceRepository = organizationServiceRepository;
            this.metadataExtractor = new ScriptMetadataExtractor(organizationServiceRepository);
        }
        public async Task<CommandResult> ExecuteAsync(ScriptTableCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? new List<string>();
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            output.WriteLine("Step 1: Extracting table metadata...");
            var entity = await metadataExtractor.GetTableAsync(command.TableName, prefixes);
            if (entity == null)
            {
                output.WriteLine($"Table not found: {command.TableName}", ConsoleColor.Red);
                return CommandResult.Fail($"Table not found: {command.TableName}");
            }
            return await ScriptExtractionHelper.ExecuteScriptExtractionAsync(
                output,
                metadataExtractor,
                prefixes,
                () => Task.FromResult(new List<Models.EntityMetadata> { entity }),
                (prefixes) => metadataExtractor.GetOptionSetsAsync(new List<string>() { command.TableName }),
                outputDir,
                command.PacxScriptName,
                command.StateFieldsDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithStateFieldsDefinition ? (Func<List<Models.OptionSetMetadata>, string, Task>)((optionSets, csvPath) => metadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath)) : ((optionSets, csvPath) => Task.CompletedTask),
                (entities, relationships, prefix) => metadataExtractor.GeneratePacxScriptForTable(entity, prefix, relationships),
                command.WithStateFieldsDefinition
            );
        }
    }
}
