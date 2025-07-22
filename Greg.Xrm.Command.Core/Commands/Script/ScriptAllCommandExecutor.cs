using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.Helpers;

namespace Greg.Xrm.Command.Commands.Script
{
	public class ScriptAllCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository) : ICommandExecutor<ScriptAllCommand>
    {
		private readonly ScriptMetadataExtractor metadataExtractor = new(organizationServiceRepository);

		public async Task<CommandResult> ExecuteAsync(ScriptAllCommand command, CancellationToken cancellationToken)
        {
            var prefixes = command.CustomPrefixs?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList() ?? [];

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
                command.WithStateFieldsDefinition ? ((optionSets, csvPath) => ScriptMetadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath)) : ((optionSets, csvPath) => Task.CompletedTask),
                (entities, relationships, prefix) => ScriptMetadataExtractor.GeneratePacxScript(entities, relationships, prefix),
                command.WithStateFieldsDefinition
            );
        }
    }
}
