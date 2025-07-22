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
                outputDir,
                command.PacxScriptName,
                command.OptionSetDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithOptionsetDefinition ? (Func<List<string>, Task<List<Models.OptionSetMetadata>>>)(pfx => metadataExtractor.GetOptionSetsAsync(pfx)) : (pfx => Task.FromResult(new List<Models.OptionSetMetadata>())),
                (entities, relationships, prefix) => metadataExtractor.GeneratePacxScript(entities, relationships, prefix),
                command.WithOptionsetDefinition
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
            return await ScriptExtractionHelper.ExecuteScriptExtractionAsync(
                output,
                metadataExtractor,
                prefixes,
                () => metadataExtractor.GetEntitiesBySolutionAsync(command.SolutionName, prefixes),
                outputDir,
                command.PacxScriptName,
                command.OptionSetDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithOptionsetDefinition ? (Func<List<string>, Task<List<Models.OptionSetMetadata>>>)(pfx => metadataExtractor.GetOptionSetsAsync(pfx)) : (pfx => Task.FromResult(new List<Models.OptionSetMetadata>())),
                (entities, relationships, prefix) => metadataExtractor.GeneratePacxScript(entities, relationships, prefix),
                command.WithOptionsetDefinition
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
                outputDir,
                command.PacxScriptName,
                command.OptionSetDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithOptionsetDefinition
                    ? (Func<List<string>, Task<List<Models.OptionSetMetadata>>>)(pfx => metadataExtractor.GetOptionSetsAsync(pfx).ContinueWith(t => t.Result.Where(os => os.EntityName == command.TableName).ToList()))
                    : (pfx => Task.FromResult(new List<Models.OptionSetMetadata>())),
                (entities, relationships, prefix) => metadataExtractor.GeneratePacxScriptForTable(entity, prefix, relationships),
                command.WithOptionsetDefinition
            );
        }
    }
}
