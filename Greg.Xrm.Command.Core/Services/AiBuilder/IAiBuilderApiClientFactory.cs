using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.AiBuilder
{
	public interface IAiBuilderApiClientFactory
	{
		Task<IAiBuilderApiClient> CreateAsync(CancellationToken ct = default);
	}
}
