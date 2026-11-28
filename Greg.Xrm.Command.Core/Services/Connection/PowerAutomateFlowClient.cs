using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Greg.Xrm.Command.Services.Connection
{
	public interface IPowerAutomateFlowClient
	{
		Task<IReadOnlyList<PowerAutomateFlow>> ListAsync(string environmentId, CancellationToken cancellationToken);
	}

	public sealed record PowerAutomateFlow(
		[property: JsonPropertyName("name")] string? Name,
		[property: JsonPropertyName("properties")] PowerAutomateFlowProperties? Properties);

	public sealed record PowerAutomateFlowProperties(
		[property: JsonPropertyName("displayName")] string? DisplayName,
		[property: JsonPropertyName("state")] string? State,
		[property: JsonPropertyName("workflowEntityId")] string? WorkflowEntityId,
		[property: JsonPropertyName("flowSuspensionReason")] string? FlowSuspensionReason);

	internal sealed class PowerAutomateFlowClient(IFlowAccessTokenProvider tokenProvider, HttpClient httpClient) : IPowerAutomateFlowClient
	{
		private static readonly Uri ApiRoot = new("https://api.flow.microsoft.com/");
		private const int MaxRetries = 3;
		internal const string AccessRequirements = "Use a Power Platform Administrator or an environment System Administrator account. " +
			"For a service principal, register it once as a management app with pac admin application register --application-id <id>.";

		public async Task<IReadOnlyList<PowerAutomateFlow>> ListAsync(string environmentId, CancellationToken cancellationToken)
		{
			var token = await tokenProvider.GetFlowAccessTokenAsync(cancellationToken);
			Uri? pageUri = new(ApiRoot, $"providers/Microsoft.ProcessSimple/scopes/admin/environments/{Uri.EscapeDataString(environmentId)}/v2/flows?api-version=2016-11-01");
			var visited = new HashSet<Uri>();
			var flows = new List<PowerAutomateFlow>();

			while (pageUri is not null)
			{
				if (pageUri.Scheme != ApiRoot.Scheme || pageUri.Authority != ApiRoot.Authority || pageUri.UserInfo.Length != 0)
					throw new InvalidOperationException("Power Automate returned a paging link outside its API origin.");
				if (!visited.Add(pageUri))
					throw new InvalidOperationException("Power Automate returned a repeated paging link.");

				using var response = await GetPageAsync(pageUri, token, cancellationToken);
				if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
					throw new HttpRequestException($"Power Automate denied access ({(int)response.StatusCode}). {AccessRequirements}", null, response.StatusCode);

				response.EnsureSuccessStatusCode();
				var page = await response.Content.ReadFromJsonAsync<FlowPage>(cancellationToken);
				if (page?.Value is null)
					throw new InvalidOperationException("Power Automate returned a page without a flow list.");

				flows.AddRange(page.Value);
				var nextLink = page.NextLink ?? page.ODataNextLink;
				pageUri = string.IsNullOrWhiteSpace(nextLink) ? null : new Uri(pageUri, nextLink);
			}

			return flows;
		}

		private async Task<HttpResponseMessage> GetPageAsync(Uri uri, string token, CancellationToken cancellationToken)
		{
			for (var retry = 0; ; retry++)
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, uri);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var response = await httpClient.SendAsync(request, cancellationToken);
				if (response.StatusCode != HttpStatusCode.TooManyRequests || retry == MaxRetries)
					return response;

				var retryAfter = response.Headers.RetryAfter;
				var delay = retryAfter?.Delta ??
					(retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : TimeSpan.FromSeconds(1 << retry));
				response.Dispose();
				if (delay > TimeSpan.Zero)
					await Task.Delay(delay, cancellationToken);
			}
		}

		private sealed record FlowPage(
			[property: JsonPropertyName("value")] List<PowerAutomateFlow>? Value,
			[property: JsonPropertyName("nextLink")] string? NextLink,
			[property: JsonPropertyName("@odata.nextLink")] string? ODataNextLink);
	}
}
