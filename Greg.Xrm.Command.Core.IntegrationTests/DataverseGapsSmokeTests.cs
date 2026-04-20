using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Greg.Xrm.Command.IntegrationTests
{
    /// <summary>
    /// Smoke tests for Dataverse Platform Gaps: Custom API, Catalog, Elastic Tables.
    /// </summary>
    [TestClass]
    public class DataverseGapsSmokeTests : IntegrationTestBase
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_CustomApiAndCatalog_ShouldRoundTrip()
        {
            if (!IsConnected) return;

            var apiName = CreateTestName("CustomApi").Replace("-", "_");
            var catalogItemName = CreateTestName("CatalogItem").Replace("-", "_");

            try
            {
                // 1. Create Custom API
                var customApi = new Microsoft.Xrm.Sdk.Entity("customapi");
                customApi["uniquename"] = apiName;
                customApi["displayname"] = "Smoke Test API";
                customApi["iscustomapivisible"] = true;
                customApi["iscustomprocessed"] = false;
                customApi["customapirequestprocessingtype"] = 2; // Global

                var apiId = CrmService!.Create(customApi);
                Assert.AreNotEqual(Guid.Empty, apiId, "Should create Custom API");
                TestContext.WriteLine($"Created Custom API: {apiName} (ID: {apiId})");

                // 2. Create Input Parameter for Custom API
                var inputParam = new Microsoft.Xrm.Sdk.Entity("customapirequestparameter");
                inputParam["customapiid"] = new Microsoft.Xrm.Sdk.EntityReference("customapi", apiId);
                inputParam["uniquename"] = "Input1";
                inputParam["displayname"] = "Input 1";
                inputParam["type"] = 10; // String
                var inputId = CrmService.Create(inputParam);
                Assert.AreNotEqual(Guid.Empty, inputId, "Should create Input Parameter");

                // 3. Create Catalog Item for the Custom API
                var catalogItem = new Microsoft.Xrm.Sdk.Entity("catalogitem");
                catalogItem["uniquename"] = catalogItemName;
                catalogItem["displayname"] = "Smoke Test Catalog Item";
                // catalogitemtype: 1 = BusinessEvent
                catalogItem["catalogitemtype"] = new Microsoft.Xrm.Sdk.OptionSetValue(1); 
                
                var catalogId = CrmService.Create(catalogItem);
                Assert.AreNotEqual(Guid.Empty, catalogId, "Should create Catalog Item");
                TestContext.WriteLine($"Created Catalog Item: {catalogItemName} (ID: {catalogId})");

                // 4. Verify they exist
                var apiQuery = new QueryExpression("customapi") { Criteria = new FilterExpression() };
                apiQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, apiName);
                var apiResult = CrmService.RetrieveMultiple(apiQuery);
                Assert.AreEqual(1, apiResult.Entities.Count, "Custom API should exist");

                var catalogQuery = new QueryExpression("catalogitem") { Criteria = new FilterExpression() };
                catalogQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, catalogItemName);
                var catalogResult = CrmService.RetrieveMultiple(catalogQuery);
                Assert.AreEqual(1, catalogResult.Entities.Count, "Catalog Item should exist");
            }
            finally
            {
                // Cleanup
                try
                {
                    var catalogQuery = new QueryExpression("catalogitem") { Criteria = new FilterExpression() };
                    catalogQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, catalogItemName);
                    var catalogResult = CrmService!.RetrieveMultiple(catalogQuery);
                    foreach (var e in catalogResult.Entities) CrmService.Delete("catalogitem", e.Id);
                }
                catch { }

                try
                {
                    var apiQuery = new QueryExpression("customapi") { Criteria = new FilterExpression() };
                    apiQuery.Criteria.AddCondition("uniquename", ConditionOperator.Equal, apiName);
                    var apiResult = CrmService!.RetrieveMultiple(apiQuery);
                    foreach (var e in apiResult.Entities) CrmService.Delete("customapi", e.Id);
                }
                catch { }
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_ElasticTable_ShouldRetrieveMetadata()
        {
            if (!IsConnected) return;

            // Placeholder for elastic table metadata verification
            TestContext.WriteLine("Elastic Table smoke test placeholder - would verify metadata settings.");
        }
    }
}
