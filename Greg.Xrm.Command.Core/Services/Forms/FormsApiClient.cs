using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.Forms
{
	public class FormsApiClient : IFormsApiClient
	{
		private readonly IFormsTokenManager _tokenManager;
		private readonly HttpClient _httpClient;

		public FormsApiClient(IFormsTokenManager tokenManager, HttpClient? httpClient = null)
		{
			_tokenManager = tokenManager;
			_httpClient = httpClient ?? new HttpClient();
		}

		public async Task<IEnumerable<FormInfo>> ListFormsAsync(string tenantId, string? ownerId, CancellationToken ct = default)
		{
			var token = await _tokenManager.GetAccessTokenAsync(ct);
			var owner = ownerId ?? "me";
			var url = $"https://forms.office.com/formapi/api/{tenantId}/users/{owner}/light/forms";

			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var forms = await response.Content.ReadFromJsonAsync<FormsListResponse>(cancellationToken: ct);
			
			var result = new List<FormInfo>();
			if (forms?.Value != null)
			{
				foreach (var form in forms.Value)
				{
					result.Add(new FormInfo
					{
						FormId = form.Id ?? "",
						Title = form.Title ?? "",
						Status = form.State ?? "Unknown",
						ResponseCount = form.RowCount,
						OwnerId = owner
					});
				}
			}
			return result;
		}

		public async Task<int> GetResponseCountAsync(string tenantId, string ownerId, string formId, CancellationToken ct = default)
		{
			var token = await _tokenManager.GetAccessTokenAsync(ct);
			var url = $"https://forms.office.com/formapi/api/{tenantId}/users/{ownerId}/light/forms('{formId}')?$select=rowCount";

			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var form = await response.Content.ReadFromJsonAsync<FormDetailResponse>(cancellationToken: ct);
			return form?.RowCount ?? 0;
		}

		public async Task<IEnumerable<ResponseInfo>> ExportResponsesAsync(string tenantId, string ownerId, string formId, int skip = 0, int top = 100, CancellationToken ct = default)
		{
			var token = await _tokenManager.GetAccessTokenAsync(ct);
			var url = $"https://forms.office.com/formapi/api/{tenantId}/users/{ownerId}/light/forms('{formId}')/responses?$skip={skip}&$top={top}&$orderby=submittedDateTime desc";

			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await _httpClient.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var responses = await response.Content.ReadFromJsonAsync<ResponsesListResponse>(cancellationToken: ct);

			var result = new List<ResponseInfo>();
			if (responses?.Value != null)
			{
				foreach (var r in responses.Value)
				{
					result.Add(new ResponseInfo
					{
						ResponseId = r.Id ?? "",
						SubmittedAt = r.SubmittedDateTime ?? DateTime.MinValue,
						Answers = r.Answers ?? new Dictionary<string, object>()
					});
				}
			}
			return result;
		}

		private class FormsListResponse
		{
			public List<FormDetailResponse>? Value { get; set; }
		}

		private class FormDetailResponse
		{
			public string? Id { get; set; }
			public string? Title { get; set; }
			public string? State { get; set; }
			public int RowCount { get; set; }
		}

		private class ResponsesListResponse
		{
			public List<ResponseDetail>? Value { get; set; }
		}

		private class ResponseDetail
		{
			public string? Id { get; set; }
			public DateTime? SubmittedDateTime { get; set; }
			public Dictionary<string, object>? Answers { get; set; }
		}
	}
}
