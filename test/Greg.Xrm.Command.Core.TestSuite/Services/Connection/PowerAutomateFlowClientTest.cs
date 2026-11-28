using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Greg.Xrm.Command.Services.Connection
{
	[TestClass]
	public class PowerAutomateFlowClientTest
	{
		[TestMethod]
		public async Task ReadsThreePagesAndStopsAtMissingNextLink()
		{
			using var handler = new StubHandler(
				Json("""{"value":[{"name":"first","properties":{"displayName":"Example","state":"Started","workflowEntityId":"id","flowSuspensionReason":"reason"}}],"nextLink":"https://api.flow.microsoft.com/page2"}"""),
				Json("""{"value":[{"name":"second"}],"nextLink":"/page3"}"""),
				Json("""{"value":[{"name":"third"}]}"""));
			using var http = new HttpClient(handler);

			var flows = await new PowerAutomateFlowClient(CreateTokenProvider(), http).ListAsync("Default-example", CancellationToken.None);

			CollectionAssert.AreEqual(new[] { "first", "second", "third" }, flows.Select(flow => flow.Name).ToArray());
			Assert.AreEqual(new PowerAutomateFlowProperties("Example", "Started", "id", "reason"), flows[0].Properties);
			CollectionAssert.AreEqual(new[]
			{
				"https://api.flow.microsoft.com/providers/Microsoft.ProcessSimple/scopes/admin/environments/Default-example/v2/flows?api-version=2016-11-01",
				"https://api.flow.microsoft.com/page2",
				"https://api.flow.microsoft.com/page3"
			}, handler.Requests);
		}

		[TestMethod]
		public async Task RetriesThrottledPageUpToThreeTimes()
		{
			using var handler = new StubHandler(Throttled(), Throttled(), Throttled(), Throttled());
			using var http = new HttpClient(handler);
			var client = new PowerAutomateFlowClient(CreateTokenProvider(), http);

			var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.ListAsync("test", CancellationToken.None));

			Assert.AreEqual(HttpStatusCode.TooManyRequests, error.StatusCode);
			Assert.AreEqual(4, handler.Requests.Count);
			Assert.AreEqual(1, handler.Requests.Distinct().Count());
		}

		[TestMethod]
		[DataRow(HttpStatusCode.Unauthorized)]
		[DataRow(HttpStatusCode.Forbidden)]
		public async Task AccessErrorsExplainRequiredPermissions(HttpStatusCode status)
		{
			using var handler = new StubHandler(new HttpResponseMessage(status));
			using var http = new HttpClient(handler);
			var client = new PowerAutomateFlowClient(CreateTokenProvider(), http);

			var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.ListAsync("test", CancellationToken.None));

			Assert.AreEqual(status, error.StatusCode);
			StringAssert.Contains(error.Message, "Power Platform Administrator");
			StringAssert.Contains(error.Message, "System Administrator");
			StringAssert.Contains(error.Message, "pac admin application register --application-id <id>");
		}

		[TestMethod]
		public async Task DoesNotSendTokenToAnotherOrigin()
		{
			using var handler = new StubHandler(Json("""{"value":[],"nextLink":"https://example.test/flows"}"""));
			using var http = new HttpClient(handler);
			var client = new PowerAutomateFlowClient(CreateTokenProvider(), http);

			await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => client.ListAsync("test", CancellationToken.None));

			Assert.AreEqual(1, handler.Requests.Count);
		}

		private static IFlowAccessTokenProvider CreateTokenProvider()
		{
			var provider = new Mock<IFlowAccessTokenProvider>(MockBehavior.Strict);
			provider.Setup(x => x.GetFlowAccessTokenAsync(CancellationToken.None)).ReturnsAsync("test-token");
			return provider.Object;
		}

		private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
		{
			Content = new StringContent(content, Encoding.UTF8, "application/json")
		};

		private static HttpResponseMessage Throttled() => new(HttpStatusCode.TooManyRequests)
		{
			Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero) }
		};

		private sealed class StubHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
		{
			public List<string> Requests { get; } = [];

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				Assert.AreEqual(HttpMethod.Get, request.Method);
				Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
				Assert.AreEqual("test-token", request.Headers.Authorization?.Parameter);
				var response = responses[Requests.Count];
				Requests.Add(request.RequestUri!.AbsoluteUri);
				return Task.FromResult(response);
			}
		}
	}
}
