using Greg.Xrm.Command.Commands.Auth;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.IntegrationTests
{
    /// <summary>
    /// Smoke tests for core operations: auth, org list, solution list, CRUD.
    /// These tests run against a real Dataverse environment when configured.
    /// </summary>
    [TestClass]
    public class CoreSmokeTests : IntegrationTestBase
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_AuthConnection_ShouldConnect()
        {
            if (!IsConnected) return;

            Assert.IsTrue(CrmService!.IsReady, "Should be connected to Dataverse");
            Assert.IsNotNull(CrmService.EnvironmentId, "Should have environment ID");
            Assert.IsNotNull(CrmService.ConnectedOrgId, "Should have organization ID");

            TestContext.WriteLine($"Connected to: {CrmService.ConnectedOrgFriendlyName}");
            TestContext.WriteLine($"Environment: {CrmService.EnvironmentId}");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_OrganizationList_ShouldRetrieve()
        {
            if (!IsConnected) return;

            var orgDetails = CrmService!.ConnectedOrgDetails;
            Assert.IsNotNull(orgDetails, "Should retrieve organization details");
            TestContext.WriteLine($"Organization: {orgDetails.OrganizationId}");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_SolutionList_ShouldRetrieve()
        {
            if (!IsConnected) return;

            var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("solution");
            query.ColumnSet.AddColumn("uniquename");
            query.ColumnSet.AddColumn("friendlyname");
            query.ColumnSet.AddColumn("version");
            query.TopCount = 5;

            var result = CrmService!.RetrieveMultiple(query);

            Assert.IsNotNull(result, "Should retrieve solutions");
            Assert.IsTrue(result.Entities.Count > 0, "Should have at least one solution");

            foreach (var solution in result.Entities)
            {
                var name = solution.GetAttributeValue<string>("uniquename");
                var version = solution.GetAttributeValue<string>("version");
                TestContext.WriteLine($"Solution: {name} v{version}");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_TableCreateDelete_ShouldRoundTrip()
        {
            if (!IsConnected) return;

            var tableName = CreateTestName("smoketable").ToLowerInvariant();

            try
            {
                // Create table
                var table = new Microsoft.Xrm.Sdk.Entity("entity");
                table["logicalname"] = tableName;
                table["displayname"] = new Microsoft.Xrm.Sdk.StringAttributeMetadata { Value = "Smoke Test Table" };
                table["description"] = "Created by integration smoke test";
                table["isactivity"] = false;
                table["schemaType"] = "Standard";

                var tableId = CrmService!.Create(table);
                Assert.IsTrue(tableId != Guid.Empty, "Should create table");
                TestContext.WriteLine($"Created table: {tableName} (ID: {tableId})");

                // Verify table exists
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("entity");
                query.ColumnSet.AddColumn("logicalname");
                query.Criteria.AddCondition("logicalname", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, tableName);

                var result = CrmService.RetrieveMultiple(query);
                Assert.IsTrue(result.Entities.Count > 0, "Should find created table");

                // Delete table
                CrmService.Delete("entity", tableId);
                TestContext.WriteLine($"Deleted table: {tableName}");

                // Verify table is deleted
                result = CrmService.RetrieveMultiple(query);
                Assert.IsTrue(result.Entities.Count == 0, "Should not find deleted table");
            }
            finally
            {
                // Cleanup — ensure table is deleted even if test fails
                try
                {
                    var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("entity");
                    query.Criteria.AddCondition("logicalname", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, tableName);
                    var result = CrmService!.RetrieveMultiple(query);
                    if (result.Entities.Count > 0)
                    {
                        CrmService.Delete("entity", result.Entities[0].Id);
                        TestContext.WriteLine($"Cleanup: Deleted table {tableName}");
                    }
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SmokeTest_PublisherCreateDelete_ShouldRoundTrip()
        {
            if (!IsConnected) return;

            var publisherName = CreateTestName("smokepub").ToLowerInvariant();

            try
            {
                // Create publisher
                var publisher = new Microsoft.Xrm.Sdk.Entity("publisher");
                publisher["uniquename"] = publisherName;
                publisher["friendlyname"] = "Smoke Test Publisher";
                publisher["customizationprefix"] = "st";
                publisher["email"] = "smoketest@pacx.local";

                var publisherId = CrmService!.Create(publisher);
                Assert.IsTrue(publisherId != Guid.Empty, "Should create publisher");
                TestContext.WriteLine($"Created publisher: {publisherName} (ID: {publisherId})");

                // Verify publisher exists
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("publisher");
                query.ColumnSet.AddColumn("uniquename");
                query.Criteria.AddCondition("uniquename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, publisherName);

                var result = CrmService.RetrieveMultiple(query);
                Assert.IsTrue(result.Entities.Count > 0, "Should find created publisher");

                // Delete publisher
                CrmService.Delete("publisher", publisherId);
                TestContext.WriteLine($"Deleted publisher: {publisherName}");
            }
            finally
            {
                // Cleanup
                try
                {
                    var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("publisher");
                    query.Criteria.AddCondition("uniquename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, CreateTestName("smokepub").ToLowerInvariant());
                    var result = CrmService!.RetrieveMultiple(query);
                    if (result.Entities.Count > 0)
                    {
                        CrmService.Delete("publisher", result.Entities[0].Id);
                    }
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
}
