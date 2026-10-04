using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	[TestClass]
	public class BusinessUnitRepositoryTest
	{
		[TestMethod]
		public async Task GetAllShouldRetrieveEveryPageAndMapParents()
		{
			var rootId = Guid.NewGuid();
			var childId = Guid.NewGuid();
			var pages = new List<int>();
			using var cancellation = new CancellationTokenSource();
			var crm = new Mock<IOrganizationServiceAsync2>(MockBehavior.Strict);
			crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token))
				.Returns((QueryBase query, CancellationToken _) =>
				{
					var expression = (QueryExpression)query;
					Assert.AreEqual("businessunit", expression.EntityName);
					CollectionAssert.AreEquivalent(new[] { "name", "parentbusinessunitid" }, expression.ColumnSet.Columns.ToArray());
					Assert.IsEmpty(expression.Criteria.Conditions);
					Assert.AreEqual("businessunitid", expression.Orders.Single().AttributeName);
					pages.Add(expression.PageInfo.PageNumber);
					var first = expression.PageInfo.PageNumber == 1;
					if (!first) Assert.AreEqual("cookie", expression.PageInfo.PagingCookie);
					return Task.FromResult(new EntityCollection([first
						? new Entity("businessunit", rootId) { ["name"] = "Root" }
						: new Entity("businessunit", childId) { ["name"] = "Child", ["parentbusinessunitid"] = new EntityReference("businessunit", rootId) }])
					{ MoreRecords = first, PagingCookie = first ? "cookie" : null });
				});

			var units = await new BusinessUnit.Repository().GetAllAsync(crm.Object, cancellation.Token);

			CollectionAssert.AreEqual(new[] { 1, 2 }, pages);
			Assert.HasCount(2, units);
			Assert.AreEqual(rootId, units[0].Id);
			Assert.IsNull(units[0].parentbusinessunitid);
			Assert.AreEqual("Child", units[1].name);
			Assert.AreEqual(rootId, units[1].parentbusinessunitid?.Id);
			crm.VerifyAll();
		}

		[TestMethod]
		public async Task GetAllShouldCancelBeforeReading()
		{
			var crm = new Mock<IOrganizationServiceAsync2>(MockBehavior.Strict);
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				new BusinessUnit.Repository().GetAllAsync(crm.Object, cancellation.Token));
			crm.VerifyNoOtherCalls();
		}
	}
}