using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.Forms
{
	public interface IFormsApiClient
	{
		Task<IEnumerable<FormInfo>> ListFormsAsync(string tenantId, string? ownerId, CancellationToken ct = default);
		Task<int> GetResponseCountAsync(string tenantId, string ownerId, string formId, CancellationToken ct = default);
		Task<IEnumerable<ResponseInfo>> ExportResponsesAsync(string tenantId, string ownerId, string formId, int skip = 0, int top = 100, CancellationToken ct = default);
	}

	public class FormInfo
	{
		public string FormId { get; set; } = "";
		public string Title { get; set; } = "";
		public string Status { get; set; } = "";
		public int ResponseCount { get; set; }
		public string OwnerId { get; set; } = "";
	}

	public class ResponseInfo
	{
		public string ResponseId { get; set; } = "";
		public DateTime SubmittedAt { get; set; }
		public Dictionary<string, object> Answers { get; set; } = new();
	}
}
