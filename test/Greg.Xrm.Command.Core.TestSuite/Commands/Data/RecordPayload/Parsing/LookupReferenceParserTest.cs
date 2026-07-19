using Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Data.RecordPayload.Parsing
{
	[TestClass]
	public class LookupReferenceParserTest
	{
		private Mock<IOrganizationServiceAsync2> _crmMock = null!;

		[TestInitialize]
		public void Setup()
		{
			_crmMock = new Mock<IOrganizationServiceAsync2>();
		}

		[TestMethod]
		public async Task ParseAsync_WithValidGuid_ShouldReturnEntityReferenceWithoutCallingCrm()
		{
			var guid = Guid.NewGuid();
			var rawValue = $"account({guid})";

			var result = await LookupReferenceParser.ParseAsync(rawValue, "parentaccountid", _crmMock.Object, CancellationToken.None);

			Assert.AreEqual("account", result.LogicalName);
			Assert.AreEqual(guid, result.Id);
			// CRM should NOT have been called
			_crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
			_crmMock.Verify(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ParseAsync_WithFieldBasedLookup_OneResult_ShouldReturnResolvedGuid()
		{
			var expectedGuid = Guid.NewGuid();
			var rawValue = "systemuser(domainname='mario@contoso.com')";

			SetupMetadata("systemuser", "systemuserid");
			SetupRetrieveMultiple("systemuser", expectedGuid);

			var result = await LookupReferenceParser.ParseAsync(rawValue, "ownerid", _crmMock.Object, CancellationToken.None);

			Assert.AreEqual("systemuser", result.LogicalName);
			Assert.AreEqual(expectedGuid, result.Id);
		}

		[TestMethod]
		public async Task ParseAsync_WithFieldBasedLookup_NoResults_ShouldThrowInvalidOperationException()
		{
			var rawValue = "systemuser(domainname='nobody@contoso.com')";

			SetupMetadata("systemuser", "systemuserid");
			SetupRetrieveMultiple("systemuser"); // no results

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => LookupReferenceParser.ParseAsync(rawValue, "ownerid", _crmMock.Object, CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("No systemuser record found"));
		}

		[TestMethod]
		public async Task ParseAsync_WithFieldBasedLookup_MultipleResults_ShouldThrowInvalidOperationException()
		{
			var rawValue = "account(name='Acme')";

			SetupMetadata("account", "accountid");
			SetupRetrieveMultiple("account", Guid.NewGuid(), Guid.NewGuid());

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => LookupReferenceParser.ParseAsync(rawValue, "parentaccountid", _crmMock.Object, CancellationToken.None));

			Assert.IsTrue(ex.Message.Contains("Ambiguous lookup"));
			Assert.IsTrue(ex.Message.Contains("2"));
		}

		[TestMethod]
		public async Task ParseAsync_WithInvalidFormat_ShouldThrowFormatException()
		{
			var rawValue = "foo bar"; // not a valid lookup reference

			await Assert.ThrowsAsync<FormatException>(
				() => LookupReferenceParser.ParseAsync(rawValue, "ownerid", _crmMock.Object, CancellationToken.None));
		}

		[TestMethod]
		public async Task ParseAsync_WithEscapedQuoteInValue_ShouldPassUnescapedValueToQuery()
		{
			// entity(name='Riccardo''s Corp') → queries for name = "Riccardo's Corp"
			var expectedGuid = Guid.NewGuid();
			var rawValue = "account(name='Riccardo''s Corp')";

			SetupMetadata("account", "accountid");

			// Capture the query to verify the unescaped value was used
			QueryExpression? capturedQuery = null;
			_crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.Callback<QueryBase>(q => capturedQuery = q as QueryExpression)
				.ReturnsAsync(new EntityCollection(
					new List<Entity> { new Entity("account") { Id = expectedGuid } }));

			var result = await LookupReferenceParser.ParseAsync(rawValue, "parentaccountid", _crmMock.Object, CancellationToken.None);

			Assert.AreEqual(expectedGuid, result.Id);
			Assert.IsNotNull(capturedQuery);
			var condition = capturedQuery!.Criteria.Conditions[0];
			Assert.AreEqual("Riccardo's Corp", condition.Values[0]);
		}

		#region Helpers

		private void SetupMetadata(string entityName, string primaryKey)
		{
			var entityMetadata = new EntityMetadata();

			// Set PrimaryIdAttribute via reflection (it's read-only)
			typeof(EntityMetadata)
				.GetProperty(nameof(EntityMetadata.PrimaryIdAttribute))!
				.SetValue(entityMetadata, primaryKey);

			var response = new RetrieveEntityResponse();
			response.Results["EntityMetadata"] = entityMetadata;

			_crmMock
				.Setup(c => c.ExecuteAsync(It.Is<OrganizationRequest>(r => r is RetrieveEntityRequest)))
				.ReturnsAsync(response);
		}

		private void SetupRetrieveMultiple(string entityName, params Guid[] guids)
		{
			var entities = guids
				.Select(g => new Entity(entityName) { Id = g })
				.ToList();

			_crmMock
				.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync(new EntityCollection(entities));
		}

		#endregion
	}
}
