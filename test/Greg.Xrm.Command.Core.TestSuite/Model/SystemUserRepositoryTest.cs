using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	[TestClass]
	public class SystemUserRepositoryTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly List<QueryExpression> queries = [];
		private readonly SystemUser.Repository repository = new();

		private void Setup(params Entity[] entities)
		{
			this.crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase q, CancellationToken _) =>
				{
					this.queries.Add((QueryExpression)q);
					return Task.FromResult(new EntityCollection([.. entities]));
				});
		}

		private static Entity User(string firstName, string lastName, string domainName)
		{
			var e = new Entity("systemuser", Guid.NewGuid());
			e["firstname"] = firstName;
			e["lastname"] = lastName;
			e["domainname"] = domainName;
			e["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) { Name = "Root" };
			return e;
		}

		[TestMethod]
		public async Task SearchAsyncWithoutQueryShouldExcludeDisabledAndApplicationUsersByDefault()
		{
			Setup(User("B", "Bianchi", "b"), User("A", "Alberti", "a"));

			var result = await this.repository.SearchAsync(this.crmMock.Object, "  ");

			var conditions = this.queries[0].Criteria.Conditions;
			Assert.AreEqual(2, conditions.Count);
			Assert.IsTrue(conditions.Any(c => c.AttributeName == "isdisabled" && c.Operator == ConditionOperator.Equal && Equals(c.Values[0], false)));
			Assert.IsTrue(conditions.Any(c => c.AttributeName == "applicationid" && c.Operator == ConditionOperator.Null));
			Assert.AreEqual(0, this.queries[0].Criteria.Filters.Count);
			CollectionAssert.AreEqual(new[] { "Alberti", "Bianchi" }, result.Select(x => x.LastName).ToArray());
			Assert.AreEqual("Root", result[0].BusinessUnitName);
		}

		[TestMethod]
		public async Task SearchAsyncWithIncludeOptionsShouldNotFilter()
		{
			Setup();

			await this.repository.SearchAsync(this.crmMock.Object, null, new SystemUserSearchOptions(IncludeDisabled: true, IncludeApplicationUsers: true));

			Assert.AreEqual(0, this.queries[0].Criteria.Conditions.Count);
			Assert.AreEqual(0, this.queries[0].Criteria.Filters.Count);
		}

		[TestMethod]
		public async Task SearchAsyncWithTopShouldUseTopCountAndServerSideOrder()
		{
			Setup(User("A", "Alberti", "a"));

			var result = await this.repository.SearchAsync(this.crmMock.Object, null, new SystemUserSearchOptions(Top: 3));

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(1, this.queries.Count);
			Assert.AreEqual(3, this.queries[0].TopCount);
			Assert.IsNull(this.queries[0].PageInfo?.PagingCookie);
			CollectionAssert.AreEqual(new[] { "lastname", "firstname", "systemuserid" }, this.queries[0].Orders.Select(o => o.AttributeName).ToArray());
		}

		[TestMethod]
		public async Task SearchAsyncWithGuidShouldIgnoreDisabledAndApplicationUserFilters()
		{
			Setup();

			await this.repository.SearchAsync(this.crmMock.Object, Guid.NewGuid().ToString(), SystemUserSearchOptions.Default);

			Assert.AreEqual("systemuserid", this.queries[0].Criteria.Conditions.Single().AttributeName);
		}

		[TestMethod]
		public async Task GetByIdAsyncShouldFilterByIdAndMapUser()
		{
			var entity = User("Mario", "Rossi", "mario");
			entity["fullname"] = "Mario Rossi";
			entity["isdisabled"] = true;
			entity["applicationid"] = Guid.NewGuid();
			Setup(entity);

			var result = await this.repository.GetByIdAsync(this.crmMock.Object, entity.Id);

			Assert.IsNotNull(result);
			Assert.AreEqual(entity.Id, result.Id);
			Assert.AreEqual("Mario Rossi", result.DisplayName);
			Assert.IsTrue(result.IsDisabled);
			Assert.IsTrue(result.IsApplicationUser);
			var condition = this.queries[0].Criteria.Conditions.Single();
			Assert.AreEqual("systemuserid", condition.AttributeName);
			Assert.AreEqual(entity.Id, condition.Values[0]);
			Assert.AreEqual(1, this.queries[0].TopCount);
		}

		[TestMethod]
		public void DisplayNameShouldFallBackToDomainNameAndId()
		{
			var id = Guid.NewGuid();
			Assert.AreEqual("dom", new SystemUser(id, "dom").DisplayName);
			Assert.AreEqual(id.ToString(), new SystemUser(id).DisplayName);
		}

		[TestMethod]
		public async Task SearchAsyncWithGuidShouldFilterById()
		{
			Setup();
			var id = Guid.NewGuid();

			await this.repository.SearchAsync(this.crmMock.Object, $" {id} ");

			var condition = this.queries[0].Criteria.Conditions.Single();
			Assert.AreEqual("systemuserid", condition.AttributeName);
			Assert.AreEqual(ConditionOperator.Equal, condition.Operator);
			Assert.AreEqual(id, condition.Values[0]);
			Assert.AreEqual(0, this.queries[0].Criteria.Filters.Count);
		}

		[TestMethod]
		public async Task SearchAsyncWithTextShouldFilterWithLikeOnNamesAndDomainName()
		{
			Setup();

			await this.repository.SearchAsync(this.crmMock.Object, " ma_rio ", new SystemUserSearchOptions(true, true));

			var filter = this.queries[0].Criteria.Filters.Single();
			Assert.AreEqual(LogicalOperator.Or, filter.FilterOperator);
			CollectionAssert.AreEquivalent(new[] { "firstname", "lastname", "domainname" }, filter.Conditions.Select(c => c.AttributeName).ToArray());
			Assert.IsTrue(filter.Conditions.All(c => c.Operator == ConditionOperator.Like && Equals(c.Values[0], "%ma[_]rio%")));
		}

		[TestMethod]
		public async Task GetByDomainNameAsyncShouldReturnNullWhenNotFound()
		{
			Setup();

			var result = await this.repository.GetByDomainNameAsync(this.crmMock.Object, @"DOMAIN\ghost");

			Assert.IsNull(result);
			var condition = this.queries[0].Criteria.Conditions.Single();
			Assert.AreEqual("domainname", condition.AttributeName);
			Assert.AreEqual(1, this.queries[0].TopCount);
		}

		[TestMethod]
		public async Task GetByDomainNameOrEmailAsyncShouldUseOrFilter()
		{
			Setup(User("A", "B", "a"));

			var result = await this.repository.GetByDomainNameOrEmailAsync(this.crmMock.Object, "a@b.com", 2);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(LogicalOperator.Or, this.queries[0].Criteria.FilterOperator);
			CollectionAssert.AreEquivalent(new[] { "domainname", "internalemailaddress" }, this.queries[0].Criteria.Conditions.Select(c => c.AttributeName).ToArray());
			Assert.AreEqual(2, this.queries[0].TopCount);
		}
	}
}
