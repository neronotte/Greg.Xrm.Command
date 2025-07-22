using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Greg.Xrm.Command.Services.Connection;
using System.Text;
using System.Linq;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using Models = Greg.Xrm.Command.Commands.Script.Models;
using System.Collections.Generic;
using Greg.Xrm.Command.Commands.Script.Helpers;

namespace Greg.Xrm.Command.Commands.Script
{
    public class ScriptMetadataExtractor
    {
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        public ScriptMetadataExtractor(IOrganizationServiceRepository organizationServiceRepository)
        {
            this.organizationServiceRepository = organizationServiceRepository;
        }

        public async Task<List<Models.EntityMetadata>> GetEntitiesByPrefixAsync(List<string> prefixes)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                RetrieveAsIfPublished = false
            };
            var response = (RetrieveAllEntitiesResponse)await crm.ExecuteAsync(request);
            return EntityMetadataHelper.ExtractEntitiesByPrefix(response.EntityMetadata, prefixes);
        }

        public async Task<List<Models.EntityMetadata>> GetEntitiesBySolutionAsync(string solutionName, List<string> prefixes)
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

            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                RetrieveAsIfPublished = false
            };
            var response = (RetrieveAllEntitiesResponse)await crm.ExecuteAsync(request);
            return EntityMetadataHelper.ExtractEntitiesBySolution(response.EntityMetadata, entityIds, prefixes);
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

        public async Task<Models.EntityMetadata?> GetTableAsync(string tableName, List<string> prefixes)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var request = new RetrieveEntityRequest
            {
                LogicalName = tableName,
                EntityFilters = EntityFilters.All
            };
            var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);
            var e = response.EntityMetadata;
            return EntityMetadataHelper.ExtractEntityByName(new List<EntityMetadata>() { response.EntityMetadata }, tableName, prefixes);
        }

        public async Task<List<Models.RelationshipMetadata>> GetRelationshipsAsync(List<string> prefixes, List<Models.EntityMetadata> includedEntities = null)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Relationships,
                RetrieveAsIfPublished = false
            };
            var response = (RetrieveAllEntitiesResponse)await crm.ExecuteAsync(request);
            return RelationshipMetadataHelper.ExtractRelationships(response.EntityMetadata, prefixes, includedEntities);
        }

        public async Task<List<Models.OptionSetMetadata>> GetOptionSetsAsync(List<string> prefixes)
        {
            var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
            var globalRequest = new RetrieveAllOptionSetsRequest();
            var globalResponse = (RetrieveAllOptionSetsResponse)await crm.ExecuteAsync(globalRequest);
            var entityRequest = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Attributes,
                RetrieveAsIfPublished = false
            };
            var entityResponse = (RetrieveAllEntitiesResponse)await crm.ExecuteAsync(entityRequest);
            var globalOptionSets = globalResponse.OptionSetMetadata.OfType<OptionSetMetadata>();
            return OptionSetMetadataHelper.ExtractOptionSets(entityResponse.EntityMetadata, globalOptionSets, prefixes);
        }

        public void GenerateOptionSetCsv(List<Models.OptionSetMetadata> optionSets, string outputFilePath)
        {
            ScriptBuilderHelper.GenerateOptionSetCsv(optionSets, outputFilePath);
        }

        public string GeneratePacxScript(List<Models.EntityMetadata> entities, List<Models.RelationshipMetadata> relationships, string customPrefix)
        {
            return ScriptBuilderHelper.GeneratePacxScript(entities, relationships, customPrefix);
        }

        public string GeneratePacxScriptForTable(Models.EntityMetadata entity, string customPrefix, List<Models.RelationshipMetadata>? relationships = null)
        {
            return ScriptBuilderHelper.GeneratePacxScriptForTable(entity, customPrefix, relationships);
        }
    }
}
