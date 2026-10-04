using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Plugin
{
	[TestClass]
	public class PluginTypeRepositoryTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new(MockBehavior.Strict);
		private readonly PluginType.Repository repository = new();
		private readonly List<QueryExpression> capturedQueries = [];

		private static readonly string[] ExpectedColumns =
		[
			"plugintypeid", "name", "typename", "culture", "friendlyname", "ismanaged",
			"isworkflowactivity", "plugintypeexportkey", "publickeytoken", "version", "pluginassemblyid"
		];

		private static Entity CreatePluginTypeRow(string name = "MyPlugin.Plugin1", Guid? id = null)
		{
			var row = new Entity("plugintype", id ?? Guid.NewGuid());
			row["name"] = name;
			row["typename"] = name;
			row["friendlyname"] = name;
			row["ismanaged"] = false;
			row["isworkflowactivity"] = false;
			row["version"] = "1.0.0.0";
			row["pluginassemblyid"] = new EntityReference("pluginassembly", Guid.NewGuid());
			return row;
		}

		private void SetupRetrieveMultiple(params EntityCollection[] pages)
		{
			var index = 0;
			this.crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryExpression>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase q, CancellationToken _) =>
				{
					// the query object is mutated between pages, we need a snapshot of the relevant data
					var query = (QueryExpression)q;
					this.capturedQueries.Add(query);
					var page = pages[Math.Min(index, pages.Length - 1)];
					index++;
					return Task.FromResult(page);
				});
		}

		private static EntityCollection Page(bool moreRecords, params Entity[] entities)
		{
			var collection = new EntityCollection(entities.ToList()) { EntityName = "plugintype", MoreRecords = moreRecords };
			if (moreRecords) collection.PagingCookie = "<cookie/>";
			return collection;
		}

		private QueryExpression LastQuery => this.capturedQueries[^1];

		private static void AssertStandardColumns(QueryExpression query)
		{
			Assert.AreEqual("plugintype", query.EntityName);
			Assert.IsTrue(query.NoLock);
			CollectionAssert.AreEquivalent(ExpectedColumns, query.ColumnSet.Columns.ToArray());
		}

		// ── GetByIdAsync ────────────────────────────────────────────────────────────

		[TestMethod]
		public async Task GetByIdAsync_WhenFound_ShouldReturnPluginType()
		{
			var id = Guid.NewGuid();
			SetupRetrieveMultiple(Page(false, CreatePluginTypeRow("MyPlugin.Plugin1", id)));

			var result = await this.repository.GetByIdAsync(this.crmMock.Object, id, CancellationToken.None);

			Assert.IsNotNull(result);
			Assert.AreEqual(id, result.Id);
			Assert.AreEqual("MyPlugin.Plugin1", result.name);
		}

		[TestMethod]
		public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
		{
			SetupRetrieveMultiple(Page(false));

			var result = await this.repository.GetByIdAsync(this.crmMock.Object, Guid.NewGuid(), CancellationToken.None);

			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task GetByIdAsync_ShouldBuildProperQuery()
		{
			var id = Guid.NewGuid();
			SetupRetrieveMultiple(Page(false));

			await this.repository.GetByIdAsync(this.crmMock.Object, id, CancellationToken.None);

			var query = this.LastQuery;
			AssertStandardColumns(query);
			Assert.AreEqual(1, query.TopCount);
			Assert.AreEqual(1, query.Criteria.Conditions.Count);
			Assert.AreEqual("plugintypeid", query.Criteria.Conditions[0].AttributeName);
			Assert.AreEqual(ConditionOperator.Equal, query.Criteria.Conditions[0].Operator);
			Assert.AreEqual(id, query.Criteria.Conditions[0].Values[0]);
		}

		// ── GetByAssemblyId ─────────────────────────────────────────────────────────

		[TestMethod]
		public async Task GetByAssemblyId_ShouldReturnAllMatchingPluginTypes()
		{
			SetupRetrieveMultiple(Page(false, CreatePluginTypeRow("A"), CreatePluginTypeRow("B")));

			var result = await this.repository.GetByAssemblyId(this.crmMock.Object, Guid.NewGuid(), CancellationToken.None);

			Assert.AreEqual(2, result.Length);
			CollectionAssert.AreEqual(new[] { "A", "B" }, result.Select(x => x.name).ToArray());
		}

		[TestMethod]
		public async Task GetByAssemblyId_ShouldBuildProperQuery()
		{
			var assemblyId = Guid.NewGuid();
			SetupRetrieveMultiple(Page(false));

			await this.repository.GetByAssemblyId(this.crmMock.Object, assemblyId, CancellationToken.None);

			var query = this.LastQuery;
			AssertStandardColumns(query);
			Assert.AreEqual(1, query.Criteria.Conditions.Count);
			Assert.AreEqual("pluginassemblyid", query.Criteria.Conditions[0].AttributeName);
			Assert.AreEqual(ConditionOperator.Equal, query.Criteria.Conditions[0].Operator);
			Assert.AreEqual(assemblyId, query.Criteria.Conditions[0].Values[0]);
		}

		// ── FuzzySearchAsync ────────────────────────────────────────────────────────

		[TestMethod]
		public async Task FuzzySearchAsync_WithoutWildcard_ShouldUseEndsWith()
		{
			SetupRetrieveMultiple(Page(false, CreatePluginTypeRow()));

			var result = await this.repository.FuzzySearchAsync(this.crmMock.Object, "Plugin1", CancellationToken.None);

			Assert.AreEqual(1, result.Length);
			var condition = this.LastQuery.Criteria.Conditions[0];
			AssertStandardColumns(this.LastQuery);
			Assert.AreEqual("name", condition.AttributeName);
			Assert.AreEqual(ConditionOperator.EndsWith, condition.Operator);
			Assert.AreEqual("Plugin1", condition.Values[0]);
		}

		[TestMethod]
		public async Task FuzzySearchAsync_WithAsteriskWildcard_ShouldUseLikeWithPercent()
		{
			SetupRetrieveMultiple(Page(false));

			await this.repository.FuzzySearchAsync(this.crmMock.Object, "My*Plugin*", CancellationToken.None);

			var condition = this.LastQuery.Criteria.Conditions[0];
			Assert.AreEqual(ConditionOperator.Like, condition.Operator);
			Assert.AreEqual("My%Plugin%", condition.Values[0]);
		}

		[TestMethod]
		public async Task FuzzySearchAsync_WithPercentWildcard_ShouldUseLikeAsIs()
		{
			SetupRetrieveMultiple(Page(false));

			await this.repository.FuzzySearchAsync(this.crmMock.Object, "My%Plugin", CancellationToken.None);

			var condition = this.LastQuery.Criteria.Conditions[0];
			Assert.AreEqual(ConditionOperator.Like, condition.Operator);
			Assert.AreEqual("My%Plugin", condition.Values[0]);
		}

		// ── SearchByNameAsync ───────────────────────────────────────────────────────

		[TestMethod]
		public async Task SearchByNameAsync_ShouldBuildProperQuery()
		{
			SetupRetrieveMultiple(Page(false, CreatePluginTypeRow()));

			await this.repository.SearchByNameAsync(this.crmMock.Object, "MyPlugin", ConditionOperator.BeginsWith, CancellationToken.None);

			var query = this.LastQuery;
			AssertStandardColumns(query);
			Assert.AreEqual(1, query.Criteria.Conditions.Count);
			Assert.AreEqual("name", query.Criteria.Conditions[0].AttributeName);
			Assert.AreEqual(ConditionOperator.BeginsWith, query.Criteria.Conditions[0].Operator);
			Assert.AreEqual("MyPlugin", query.Criteria.Conditions[0].Values[0]);
			Assert.AreEqual(1, query.Orders.Count);
			Assert.AreEqual("name", query.Orders[0].AttributeName);
			Assert.AreEqual(OrderType.Ascending, query.Orders[0].OrderType);
		}

		[TestMethod]
		public async Task SearchByNameAsync_ShouldRetrieveAllPages()
		{
			SetupRetrieveMultiple(
				Page(true, CreatePluginTypeRow("A"), CreatePluginTypeRow("B")),
				Page(false, CreatePluginTypeRow("C")));

			var result = await this.repository.SearchByNameAsync(this.crmMock.Object, "A", ConditionOperator.Like, CancellationToken.None);

			Assert.AreEqual(3, result.Length);
			CollectionAssert.AreEqual(new[] { "A", "B", "C" }, result.Select(x => x.name).ToArray());
			Assert.AreEqual(2, this.capturedQueries.Count);
			Assert.AreEqual(2, this.LastQuery.PageInfo.PageNumber);
			Assert.AreEqual("<cookie/>", this.LastQuery.PageInfo.PagingCookie);
		}

		[TestMethod]
		public async Task SearchByNameAsync_WhenNoResults_ShouldReturnEmptyArray()
		{
			SetupRetrieveMultiple(Page(false));

			var result = await this.repository.SearchByNameAsync(this.crmMock.Object, "nope", ConditionOperator.Equal, CancellationToken.None);

			Assert.AreEqual(0, result.Length);
		}
	}
}
