using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.Helpers;

namespace Greg.Xrm.Command.Commands.Script
{

    public class ScriptTableCommandExecutor(
        IOutput output, 
        IOrganizationServiceRepository organizationServiceRepository) 
        : ICommandExecutor<ScriptTableCommand>
    {
        private readonly ScriptMetadataExtractor metadataExtractor = new(organizationServiceRepository);

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
                (prefixes) => metadataExtractor.GetOptionSetsAsync([command.TableName]),
                outputDir,
                command.PacxScriptName,
                command.StateFieldsDefinitionName,
                (entities) => metadataExtractor.GetRelationshipsAsync(prefixes, entities),
                command.WithStateFieldsDefinition ? ((optionSets, csvPath) => ScriptMetadataExtractor.GenerateStateFieldsCSV(optionSets, csvPath)) : ((optionSets, csvPath) => Task.CompletedTask),
                (entities, relationships, prefix) => ScriptMetadataExtractor.GeneratePacxScriptForTable(entity, prefix, relationships),
                command.WithStateFieldsDefinition
            );
        }
    }
}
