using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Security.Teams
{
	[TestClass]
	public class GetRolesCommandExecutorTest
	{
		private readonly SecurityTeamTestContext context = new();
		private GetRolesCommandExecutor Executor() => new(context.Output, context.Connections.Object, context.TeamRepository, context.Roles);

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task TeamShouldResolveByNameOrGuidAndReturnAssignedRoles(bool guid)
		{
			var result = await Executor().ExecuteAsync(new GetRolesCommand { Team = guid ? context.TeamId.ToString() : " Sales " }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(context.TeamId, result["TeamId"]);
			Assert.AreEqual(2, result["Count"]);
			Assert.AreEqual("Regional, Salesperson", result["Roles"]);
			var roleQuery = context.Queries.Single(query => query.EntityName == "role");
			var link = roleQuery.LinkEntities.Single();
			Assert.AreEqual("teamroles", link.LinkToEntityName);
			Assert.AreEqual(context.TeamId, link.LinkCriteria.Conditions.Single().Values.Single());
			Assert.IsEmpty(link.LinkEntities);
			StringAssert.Contains(context.Output.ToString(), "Managed");
		}

		[TestMethod]
		public async Task TeamWithNoRolesShouldSucceed()
		{
			var result = await Executor().ExecuteAsync(new GetRolesCommand { Team = context.AccessTeamId.ToString() }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(0, result["Count"]);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task MissingOrAmbiguousTeamShouldFailBeforeReadingRoles(bool ambiguous)
		{
			if (ambiguous) context.Teams.Add(context.Team(Guid.NewGuid(), "Sales", 0));
			else context.Teams.Clear();
			var result = await Executor().ExecuteAsync(new GetRolesCommand { Team = "Sales" }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, ambiguous ? "GUID" : "not found");
			Assert.IsFalse(context.Queries.Any(query => query.EntityName == "role"));
		}

		[TestMethod]
		public async Task RoleQueryShouldReadAllPages()
		{
			var pages = new List<int>();
			context.Crm.Setup(client => client.RetrieveMultipleAsync(It.Is<QueryBase>(query => ((QueryExpression)query).EntityName == "role"), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) =>
				{
					var page = ((QueryExpression)query).PageInfo.PageNumber;
					pages.Add(page);
					return Task.FromResult(new EntityCollection([context.TeamRoles[page - 1]]) { MoreRecords = page == 1, PagingCookie = "cookie" });
				});
			var result = await Executor().ExecuteAsync(new GetRolesCommand { Team = "Sales" }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(2, result["Count"]);
			CollectionAssert.AreEqual(new[] { 1, 2 }, pages);
		}

		[TestMethod]
		public async Task RoleReadFailureShouldReturnFailure()
		{
			context.Crm.Setup(client => client.RetrieveMultipleAsync(It.Is<QueryBase>(query => ((QueryExpression)query).EntityName == "role"), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("Roles denied"));
			var result = await Executor().ExecuteAsync(new GetRolesCommand { Team = "Sales" }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Roles denied");
		}

		[TestMethod]
		public async Task CancellationShouldPropagateBeforeConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => Executor().ExecuteAsync(new GetRolesCommand { Team = "Sales" }, cancellation.Token));
			context.Connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task CancellationTokenShouldReachQueries()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await Executor().ExecuteAsync(new GetRolesCommand { Team = "Sales" }, cancellation.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			context.Crm.Verify(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token), Times.Exactly(2));
		}
	}
}