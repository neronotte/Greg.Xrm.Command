using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Greg.Xrm.Command.IntegrationTests
{
    /// <summary>
    /// Base class for integration tests that require a real Dataverse connection.
    /// Reads connection details from environment variables:
    ///   - PACX_TEST_URL: Dataverse environment URL
    ///   - PACX_TEST_CLIENT_ID: App Registration Client ID
    ///   - PACX_TEST_CLIENT_SECRET: App Registration Client Secret
    ///   - PACX_TEST_TENANT_ID: Azure AD Tenant ID (optional, defaults to common)
    ///
    /// Tests are skipped if environment variables are not set.
    /// All tests are idempotent — they create and clean up their own resources.
    /// Resources are prefixed with "pacx_test_" to avoid conflicts.
    /// </summary>
    [TestClass]
    public abstract class IntegrationTestBase
    {
        protected ServiceClient? CrmService { get; private set; }
        protected bool IsConnected { get; private set; }

        [TestInitialize]
        public void TestInitialize()
        {
            var url = Environment.GetEnvironmentVariable("PACX_TEST_URL");
            var clientId = Environment.GetEnvironmentVariable("PACX_TEST_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("PACX_TEST_CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("PACX_TEST_TENANT_ID") ?? "common";

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Assert.Inconclusive("Integration test skipped: PACX_TEST_URL, PACX_TEST_CLIENT_ID, and PACX_TEST_CLIENT_SECRET environment variables are required.");
                return;
            }

            try
            {
                CrmService = new ServiceClient(
                    new Uri(url),
                    tenantId,
                    clientId,
                    clientSecret,
                    useUniqueInstance: true);

                if (CrmService.IsReady)
                {
                    IsConnected = true;
                }
                else
                {
                    Assert.Inconclusive($"Integration test skipped: Could not connect to Dataverse. Error: {CrmService?.LastCrmError}");
                }
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"Integration test skipped: Failed to create ServiceClient. Error: {ex.Message}");
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CrmService?.Dispose();
        }

        /// <summary>
        /// Creates a unique test-prefixed name for resources.
        /// Format: pacx_test_{testName}_{timestamp}
        /// </summary>
        protected string CreateTestName(string testName)
        {
            return $"pacx_test_{testName}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        }
    }
}
