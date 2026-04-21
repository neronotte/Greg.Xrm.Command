using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Services.AiBuilder
{
	public class AiBuilderApiClientFactory : IAiBuilderApiClientFactory
	{
		private readonly IOrganizationServiceRepository _organizationServiceRepository;
		private readonly ITokenProvider _tokenProvider;
		private readonly HttpClient _httpClient;

		public AiBuilderApiClientFactory(
			IOrganizationServiceRepository organizationServiceRepository,
			ITokenProvider tokenProvider,
			HttpClient? httpClient = null)
		{
			_organizationServiceRepository = organizationServiceRepository;
			_tokenProvider = tokenProvider;
			_httpClient = httpClient ?? new HttpClient();
		}

		public async Task<IAiBuilderApiClient> CreateAsync(CancellationToken ct = default)
		{
			var crm = await _organizationServiceRepository.GetCurrentConnectionAsync(ct);
			
			if (crm is ServiceClient serviceClient)
			{
				return new AiBuilderApiClient(serviceClient, _tokenProvider, _httpClient);
			}

			throw new InvalidOperationException("AI Builder API requires a ServiceClient connection.");
		}
	}
}
