using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.MetadataExtractor
{
    public interface IScriptMetadataExtractor
    {
        string GeneratePacxScript(List<Extractor_EntityMetadata> entities, List<Extractor_RelationshipMetadata> relationships, List<string> prefixes);
        Task GenerateStateFieldsCSV(List<Extractor_OptionSetMetadata> optionSets, string outputFilePath);
        Task<List<Extractor_EntityMetadata>> GetEntitiesByPrefixAsync(List<string> prefixes);
        Task<List<Extractor_EntityMetadata>> GetEntitiesBySolutionAsync(string solutionName, List<string> prefixes);
        Task<List<Extractor_OptionSetMetadata>> GetOptionSetsAsync(List<string>? entityFilter = null);
        Task<List<Extractor_RelationshipMetadata>> GetRelationshipsAsync(List<string> prefixes, List<Extractor_EntityMetadata>? includedEntities = null);
        Task<Extractor_EntityMetadata?> GetTableAsync(string tableName, List<string> prefixes);
    }
}