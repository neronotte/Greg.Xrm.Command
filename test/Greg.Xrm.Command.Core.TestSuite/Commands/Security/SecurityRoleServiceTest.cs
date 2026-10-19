using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class SecurityRoleServiceTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly List<QueryExpression> queries = [];
		private readonly SecurityRoleService service = new();

		private void Setup(Func<QueryExpression, EntityCollection> responder)
		{
			this.crmMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase q, CancellationToken _) =>
				{
					var query = (QueryExpression)q;
					this.queries.Add(query);
					return Task.FromResult(responder(query));
				});
		}

		private static Entity Role(Guid id, string name, bool managed = false)
		{
			var e = new Entity("role", id);
			e["name"] = name;
			e["ismanaged"] = managed;
			e["businessunitid"] = new EntityReference("businessunit", Guid.NewGuid()) { Name = "Root" };
			return e;
		}

		[TestMethod]
		public async Task GetRolesAsyncShouldExcludeBusinessUnitCopies()
		{
			Setup(_ => new EntityCollection([Role(Guid.NewGuid(), "B"), Role(Guid.NewGuid(), "A")]));

			var result = await this.service.GetRolesAsync(this.crmMock.Object, false, CancellationToken.None);

			Assert.AreEqual(1, this.queries.Count);
			var conditions = this.queries[0].Criteria.Conditions;
			Assert.IsTrue(conditions.Any(c => c.AttributeName == "parentroleid" && c.Operator == ConditionOperator.Null));
			Assert.IsFalse(conditions.Any(c => c.AttributeName == "ismanaged"));
			CollectionAssert.AreEqual(new[] { "A", "B" }, result.Select(x => x.Name).ToArray());
		}

		[TestMethod]
		public async Task GetRolesAsyncWithUnmanagedOnlyShouldFilterOnIsManaged()
		{
			Setup(_ => new EntityCollection());

			await this.service.GetRolesAsync(this.crmMock.Object, true, CancellationToken.None);

			Assert.IsTrue(this.queries[0].Criteria.Conditions.Any(c => c.AttributeName == "ismanaged" && Equals(c.Values[0], false)));
		}

		[TestMethod]
		public async Task GetRolesAsyncWithNameShouldFilterWithLikeAndEscapeWildcards()
		{
			Setup(_ => new EntityCollection());

			await this.service.GetRolesAsync(this.crmMock.Object, false, " 50%_sales ", CancellationToken.None);

			var condition = this.queries[0].Criteria.Conditions.Single(c => c.AttributeName == "name");
			Assert.AreEqual(ConditionOperator.Like, condition.Operator);
			Assert.AreEqual("%50[%][_]sales%", condition.Values[0]);
		}

		[TestMethod]
		public async Task GetRolesAsyncWithoutNameShouldNotFilterOnName()
		{
			Setup(_ => new EntityCollection());

			await this.service.GetRolesAsync(this.crmMock.Object, false, "  ", CancellationToken.None);

			Assert.IsFalse(this.queries[0].Criteria.Conditions.Any(c => c.AttributeName == "name"));
		}

		[TestMethod]
		public async Task GetRolesAsyncShouldReadAllPages()
		{
			var page = 0;
			Setup(_ =>
			{
				page++;
				return new EntityCollection([Role(Guid.NewGuid(), $"R{page}")])
				{
					MoreRecords = page == 1,
					PagingCookie = page == 1 ? "<cookie/>" : null
				};
			});

			var result = await this.service.GetRolesAsync(this.crmMock.Object, false, CancellationToken.None);

			Assert.AreEqual(2, result.Count);
		}

		[TestMethod]
		public async Task GetRolesByUserAsyncShouldUseTwoQueriesAndMergeSources()
		{
			var shared = Guid.NewGuid();
			var directOnly = Guid.NewGuid();
			var teamOnly = Guid.NewGuid();

			Setup(q => q.LinkEntities[0].LinkToEntityName == "systemuserroles"
				? new EntityCollection([Role(shared, "Shared"), Role(directOnly, "Direct")])
				: new EntityCollection([Role(shared, "Shared"), Role(teamOnly, "Team")]));

			var result = await this.service.GetRolesByUserAsync(this.crmMock.Object, Guid.NewGuid(), CancellationToken.None);

			Assert.AreEqual(2, this.queries.Count, "Expected exactly one query for direct roles and one for team roles");
			var teamQuery = this.queries.Single(q => q.LinkEntities[0].LinkToEntityName == "teamroles");
			Assert.AreEqual("teammembership", teamQuery.LinkEntities[0].LinkEntities[0].LinkToEntityName);

			Assert.AreEqual(3, result.Count);
			CollectionAssert.AreEqual(new[] { "Direct", "Team" }, result.Single(x => x.Role.RoleId == shared).Sources.ToArray());
			CollectionAssert.AreEqual(new[] { "Direct" }, result.Single(x => x.Role.RoleId == directOnly).Sources.ToArray());
			CollectionAssert.AreEqual(new[] { "Team" }, result.Single(x => x.Role.RoleId == teamOnly).Sources.ToArray());
		}
	}
}
