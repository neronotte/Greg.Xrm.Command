using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Greg.Xrm.Command.Services.AiBuilder
{
	public interface IAiBuilderApiClient
	{
		Task<IEnumerable<AiModelInfo>> ListModelsAsync(CancellationToken ct = default);
		Task TrainModelAsync(string modelId, bool wait, CancellationToken ct = default);
		Task PublishModelAsync(string modelId, CancellationToken ct = default);
		Task ConfigureFormProcessorAsync(string modelId, string documentType, string[]? fields, string[]? tables, CancellationToken ct = default);
	}

	public class AiModelInfo
	{
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
		public string Status { get; set; } = "";
		public string? ErrorMessage { get; set; }
		public DateTime? CreatedOn { get; set; }
		public DateTime? TrainedOn { get; set; }
		public double? Accuracy { get; set; }
	}
}
