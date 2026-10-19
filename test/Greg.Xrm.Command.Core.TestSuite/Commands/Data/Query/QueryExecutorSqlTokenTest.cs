namespace Greg.Xrm.Command.Commands.Data.Query
{
	/// <summary>
	/// Issue #217: the TDS (SQL) token must come from the ServiceClient that runs the query, which the SDK
	/// refreshes, with the pre-#217 repository token only as fallback for legacy fixed-token connections.
	/// </summary>
	[TestClass]
	public class QueryExecutorSqlTokenTest
	{
		[TestMethod]
		public async Task ResolveAccessToken_WhenClientHasAToken_ShouldUseItAndNotAskTheRepository()
		{
			var repositoryAsked = false;

			var token = await QueryExecutorSql.ResolveAccessTokenAsync(
				() => "client-token",
				() => { repositoryAsked = true; return Task.FromResult<string?>("repository-token"); });

			Assert.AreEqual("client-token", token);
			Assert.IsFalse(repositoryAsked, "Asking the repository would open a second connection and could return a stale token.");
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		public async Task ResolveAccessToken_WhenClientHasNoToken_ShouldFallBackToTheRepository(string? clientToken)
		{
			// ServiceClient.CurrentAccessToken is empty for fixed-token clients (legacy mode without secure storage).
			var token = await QueryExecutorSql.ResolveAccessTokenAsync(
				() => clientToken,
				() => Task.FromResult<string?>("repository-token"));

			Assert.AreEqual("repository-token", token);
		}

		[TestMethod]
		public async Task ResolveAccessToken_WhenNoTokenIsAvailable_ShouldReturnNoToken()
		{
			var token = await QueryExecutorSql.ResolveAccessTokenAsync(
				() => string.Empty,
				() => Task.FromResult<string?>(null));

			Assert.IsTrue(string.IsNullOrEmpty(token), "The caller turns this into the existing actionable error message.");
		}

		[TestMethod]
		public async Task ResolveAccessToken_ShouldReadTheClientTokenAtCallTimeEveryTime()
		{
			// The SDK may refresh the token between two queries of a long-running command: it must not be cached.
			var current = "token-1";

			var first = await QueryExecutorSql.ResolveAccessTokenAsync(() => current, () => Task.FromResult<string?>(null));
			current = "token-2-refreshed";
			var second = await QueryExecutorSql.ResolveAccessTokenAsync(() => current, () => Task.FromResult<string?>(null));

			Assert.AreEqual("token-1", first);
			Assert.AreEqual("token-2-refreshed", second);
		}
	}
}
