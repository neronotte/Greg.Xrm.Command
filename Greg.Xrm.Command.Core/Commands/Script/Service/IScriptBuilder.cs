using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.Service
{
    public interface IScriptBuilder
    {
        void GenerateOptionSetCsv(List<Extractor_OptionSetMetadata> optionSets, string outputFilePath);
        string GeneratePacxScript(List<Extractor_EntityMetadata> entities, List<Extractor_RelationshipMetadata> relationships, List<string> prefixes);
    }
}