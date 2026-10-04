using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Greg.Xrm.Command.Model;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	[TestClass]
	public class ListCommandExecutorTest
	{
		private readonly SecurityTeamTestContext context = new();
		private ListCommandExecutor Executor() => new(context.Output, context.Connections.Object, context.TeamRepository);

		[TestMethod]
		public async Task DefaultListShouldReturnEveryType()
		{
			var result = await Executor().ExecuteAsync(new ListCommand(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(3, result["Count"]);
			Assert.IsEmpty(context.Queries.Single().Criteria.Conditions);
			StringAssert.Contains(context.Output.ToString(), "SecurityGroup");
			StringAssert.Contains(context.Output.ToString(), "Access");
		}

		[TestMethod]
		[DataRow(TeamType.Owner)]
		[DataRow(TeamType.Access)]
		[DataRow(TeamType.SecurityGroup)]
		[DataRow(TeamType.Microsoft365Group)]
		public async Task FiltersShouldBeAppliedInDataverseQuery(TeamType type)
		{
			var result = await Executor().ExecuteAsync(new ListCommand { Type = type, Name = " Sales " }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var query = context.Queries.Single();
			Assert.AreEqual((int)type, query.Criteria.Conditions.Single(condition => condition.AttributeName == "teamtype").Values[0]);
			var name = query.Criteria.Conditions.Single(condition => condition.AttributeName == "name");
			Assert.AreEqual(ConditionOperator.Like, name.Operator);
			Assert.AreEqual("%Sales%", name.Values[0]);
		}

		[TestMethod]
		public async Task ListShouldReadAllPages()
		{
			context.ListPages.Add(new EntityCollection([context.Teams[0]]) { MoreRecords = true, PagingCookie = "cookie" });
			context.ListPages.Add(new EntityCollection([context.Teams[1], context.Teams[2]]));
			var result = await Executor().ExecuteAsync(new ListCommand(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(3, result["Count"]);
			CollectionAssert.AreEqual(new[] { 1, 2 }, context.ListPageNumbers);
		}

		[TestMethod]
		public async Task EmptyListShouldSucceed()
		{
			context.Teams.Clear();
			var result = await Executor().ExecuteAsync(new ListCommand(), CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, result["Count"]);
		}

		[TestMethod]
		public async Task ReadFailureShouldReturnFailure()
		{
			context.Crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Read denied"));
			var result = await Executor().ExecuteAsync(new ListCommand(), CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Read denied");
		}

		[TestMethod]
		public async Task CancellationShouldPropagateBeforeConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => Executor().ExecuteAsync(new ListCommand(), cancellation.Token));
			context.Connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}
	}
}