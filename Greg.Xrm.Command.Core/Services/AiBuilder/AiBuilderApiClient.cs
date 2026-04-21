using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.AiBuilder
{
	public class AiBuilderApiClient : IAiBuilderApiClient
	{
		private readonly ServiceClient _serviceClient;
		private readonly ITokenProvider _tokenProvider;
		private readonly HttpClient _httpClient;
		private string? _baseUrl;

		public AiBuilderApiClient(ServiceClient serviceClient, ITokenProvider tokenProvider, HttpClient? httpClient = null)
		{
			_serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
			_tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
			_httpClient = httpClient ?? new HttpClient();
		}

		private async Task<string> GetBaseUrlAsync(CancellationToken ct)
		{
			if (_baseUrl != null) return _baseUrl;

			var whoAmI = await _serviceClient.ExecuteAsync(new WhoAmIRequest(), ct) as WhoAmIResponse;
			var orgDetail = await _serviceClient.RetrieveAsync(
				"organization",
				whoAmI.UserId,
				new ColumnSet("organizationid", "weburl", "friendlyname"),
				ct);

			var webUrl = orgDetail.GetAttributeValue<string>("weburl");
			if (string.IsNullOrEmpty(webUrl))
			{
				throw new InvalidOperationException("Could not determine organization Web URL. Please ensure the environment is properly configured.");
			}

			_baseUrl = webUrl.TrimEnd('/');
			return _baseUrl;
		}

		public async Task<IEnumerable<AiModelInfo>> ListModelsAsync(CancellationToken ct = default)
		{
			var query = new QueryExpression("aimodel");
			query.ColumnSet.AddColumns(
				"aimodelid",
				"name",
				"statuscode",
				"createdon",
				"description",
				"versionnumber");
			query.AddOrder("createdon", OrderType.Descending);

			var result = await _serviceClient.RetrieveMultipleAsync(query, ct);
			var models = new List<AiModelInfo>();

			foreach (var entity in result.Entities)
			{
				var statusCode = entity.GetAttributeValue<int?>("statuscode") ?? 0;
				models.Add(new AiModelInfo
				{
					Id = entity.GetAttributeValue<Guid?>("aimodelid")?.ToString() ?? "",
					Name = entity.GetAttributeValue<string>("name") ?? "",
					Status = GetStatusText(statusCode),
					CreatedOn = entity.GetAttributeValue<DateTime?>("createdon")
				});
			}

			return models;
		}

		public async Task TrainModelAsync(string modelId, bool wait, CancellationToken ct = default)
		{
			var baseUrl = await GetBaseUrlAsync(ct);
			var aiBuilderUrl = BuildAiBuilderUrl(baseUrl);

			var token = await GetAccessTokenAsync(aiBuilderUrl, ct);

			var request = new HttpRequestMessage(HttpMethod.Post, $"{aiBuilderUrl}/models/{modelId}/train");
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync(ct);
				throw new InvalidOperationException($"Failed to train model: {response.StatusCode} - {error}");
			}

			if (wait)
			{
				await PollForTrainingComplete(modelId, aiBuilderUrl, token, ct);
			}
		}

		public async Task PublishModelAsync(string modelId, CancellationToken ct = default)
		{
			var baseUrl = await GetBaseUrlAsync(ct);
			var aiBuilderUrl = BuildAiBuilderUrl(baseUrl);

			var token = await GetAccessTokenAsync(aiBuilderUrl, ct);

			var request = new HttpRequestMessage(HttpMethod.Post, $"{aiBuilderUrl}/models/{modelId}/publish");
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync(ct);
				throw new InvalidOperationException($"Failed to publish model: {response.StatusCode} - {error}");
			}
		}

		public async Task ConfigureFormProcessorAsync(string modelId, string documentType, string[]? fields, string[]? tables, CancellationToken ct = default)
		{
			var baseUrl = await GetBaseUrlAsync(ct);
			var aiBuilderUrl = BuildAiBuilderUrl(baseUrl);

			var token = await GetAccessTokenAsync(aiBuilderUrl, ct);

			var config = new
			{
				documentType = documentType,
				fields = fields?.ToList() ?? new List<string>(),
				tables = tables?.ToList() ?? new List<string>()
			};

			var json = JsonSerializer.Serialize(config);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var request = new HttpRequestMessage(HttpMethod.Patch, $"{aiBuilderUrl}/models/{modelId}/formprocessor/config")
			{
				Content = content
			};
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync(ct);
				throw new InvalidOperationException($"Failed to configure form processor: {response.StatusCode} - {error}");
			}
		}

		private async Task<string> GetAccessTokenAsync(string resource, CancellationToken ct)
		{
			var token = await _tokenProvider.GetTokenAsync(resource, ct);
			if (string.IsNullOrEmpty(token))
			{
				throw new InvalidOperationException($"Failed to acquire token for resource: {resource}");
			}
			return token;
		}

		private string BuildAiBuilderUrl(string baseUrl)
		{
			var uri = new Uri(baseUrl);
			var authority = uri.Authority;
			
			if (authority.Contains("dynamics.com"))
			{
				var parts = authority.Split('.');
				var environment = parts[0];
				var region = parts[1];
				return $"https://{environment}.api.{region}.powerplatform.com/aiBuilder";
			}
			
			return $"{baseUrl}/api/aibbuilder";
		}

		private async Task PollForTrainingComplete(string modelId, string aiBuilderUrl, string token, CancellationToken ct)
		{
			var maxAttempts = 120;
			var delay = TimeSpan.FromSeconds(30);

			for (int i = 0; i < maxAttempts; i++)
			{
				var request = new HttpRequestMessage(HttpMethod.Get, $"{aiBuilderUrl}/models/{modelId}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await _httpClient.SendAsync(request, ct);
				if (response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync(ct);
					var doc = JsonDocument.Parse(json);
					var status = doc.RootElement.GetProperty("status").GetString();

					if (status == "Published" || status == "Ready")
					{
						return;
					}

					if (status == "Failed")
					{
						var error = doc.RootElement.TryGetProperty("errorMessage", out var errorMsg) 
							? errorMsg.GetString() 
							: "Unknown error";
						throw new InvalidOperationException($"Training failed: {error}");
					}
				}

				await Task.Delay(delay, ct);
			}

			throw new TimeoutException("Training polling timed out after 60 minutes.");
		}

		private static string GetStatusText(int statusCode) => statusCode switch
		{
			0 => "Draft",
			1 => "Training",
			2 => "Trained",
			3 => "Compiled",
			4 => "Ready",
			5 => "Published",
			192350000 => "Not Started",
			192350001 => "Training",
			192350002 => "Training Complete",
			192350003 => "Training Failed",
			192350004 => "Publishing",
			192350005 => "Published",
			192350006 => "Publish Failed",
			_ => $"Unknown ({statusCode})"
		};
	}
}
