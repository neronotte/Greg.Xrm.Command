using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Commands.Script.Service;
using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.MetadataExtractor
{
    public class ScriptMetadataExtractor : IScriptMetadataExtractor
    {
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly IScriptBuilder scriptBuilder;
        private RetrieveAllEntitiesResponse? cachedAllEntitiesResponse;
        private Task<RetrieveAllEntitiesResponse>? cachedAllEntitiesTask;

        public ScriptMetadataExtractor(IOrganizationServiceRepository organizationServiceRepository, IScriptBuilder scriptBuilder)
        {
            this.organizationServiceRepository = organizationServiceRepository;
            this.scriptBuilder = scriptBuilder;
        }

        private async Task<RetrieveAllEntitiesResponse> GetAllEntitiesResponseAsync()
        {
            if (cachedAllEntitiesResponse != null)
                return cachedAllEntitiesResponse;
            if (cachedAllEntitiesTask != null)
                return await cachedAllEntitiesTask;
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = false
            };
            cachedAllEntitiesTask = crm.ExecuteAsync(request).ContinueWith(t => (RetrieveAllEntitiesResponse)t.Result);
            cachedAllEntitiesResponse = await cachedAllEntitiesTask;
            return cachedAllEntitiesResponse;
        }

        public async Task<List<Extractor_EntityMetadata>> GetEntitiesByPrefixAsync(List<string> prefixes)
        {
            var response = await GetAllEntitiesResponseAsync();
            return Extractor_EntityMetadata.ExtractEntitiesByPrefix(response.EntityMetadata, prefixes);
        }

        public async Task<List<Extractor_EntityMetadata>> GetEntitiesBySolutionAsync(string solutionName, List<string> prefixes)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, await GetSolutionIdAsync(crm, solutionName));
            query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, 1); // 1 = Entity
            var solutionComponents = await crm.RetrieveMultipleAsync(query);
            var entityIds = solutionComponents.Entities.Select(e => (Guid)e["objectid"]).ToList();
            var response = await GetAllEntitiesResponseAsync();
            return Extractor_EntityMetadata.ExtractEntitiesBySolution(response.EntityMetadata, entityIds, prefixes);
        }

        private async Task<Guid> GetSolutionIdAsync(IOrganizationServiceAsync2 crm, string solutionName)
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);
            var solutions = await crm.RetrieveMultipleAsync(query);
            var solution = solutions.Entities.FirstOrDefault();
            if (solution == null) throw new Exception($"Solution not found: {solutionName}");
            return (Guid)solution["solutionid"];
        }

        public async Task<Extractor_EntityMetadata?> GetTableAsync(string tableName, List<string> prefixes)
        {
            var response = await GetAllEntitiesResponseAsync();
            var e = response.EntityMetadata.FirstOrDefault(x => x.LogicalName == tableName);
            if (e == null) return null;
            return Extractor_EntityMetadata.ExtractEntityByName(new List<EntityMetadata>() { e }, tableName, prefixes);
        }

        public async Task<List<Extractor_RelationshipMetadata>> GetRelationshipsAsync(List<string> prefixes, List<Extractor_EntityMetadata>? includedEntities = null)
        {
            var response = await GetAllEntitiesResponseAsync();
            return Extractor_RelationshipMetadata.ExtractRelationships(response.EntityMetadata, prefixes, includedEntities);
        }

        public async Task<List<Extractor_OptionSetMetadata>> GetOptionSetsAsync(List<string>? entityFilter = null)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var globalRequest = new RetrieveAllOptionSetsRequest();
            var globalResponse = (RetrieveAllOptionSetsResponse)await crm.ExecuteAsync(globalRequest);
            var response = await GetAllEntitiesResponseAsync();
            var globalOptionSets = globalResponse.OptionSetMetadata.OfType<OptionSetMetadata>();
            var result = Extractor_OptionSetMetadata.ExtractOptionSets(response.EntityMetadata, globalOptionSets);
            if (entityFilter != null)
            {
                return result.Where(os => entityFilter.Contains(os.EntityName)).ToList();
            }
            return result;
        }

        public Task GenerateStateFieldsCSV(List<Extractor_OptionSetMetadata> optionSets, string outputFilePath)
        {
            scriptBuilder.GenerateOptionSetCsv(optionSets, outputFilePath);
            return Task.CompletedTask;
        }

        public string GeneratePacxScript(List<Extractor_EntityMetadata> entities, List<Extractor_RelationshipMetadata> relationships, List<string> prefixes)
        {
            return scriptBuilder.GeneratePacxScript(entities, relationships, prefixes);
        }
    }
}
