using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.Helpers;

namespace Greg.Xrm.Command.Commands.Script
{
	public class ScriptSolutionCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ScriptSolutionCommand>
    {
        private readonly ScriptMetadataExtractor metadataExtractor = new(organizationServiceRepository);

		public async Task<CommandResult> ExecuteAsync(ScriptSolutionCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? [];
            var outputDir = string.IsNullOrWhiteSpace(command.OutputDir) ? Environment.CurrentDirectory : command.OutputDir;
            
            var solutionNames = command.SolutionNames?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? [];
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
                command.WithStateFieldsDefinition ? ((optionSets, csvPath) => ScriptMetadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath)) : ((optionSets, csvPath) => Task.CompletedTask),
                (entities, relationships, prefix) => ScriptMetadataExtractor.GeneratePacxScript(entities, relationships, prefix),
                command.WithStateFieldsDefinition,
                2
            );
        }
    }
}
