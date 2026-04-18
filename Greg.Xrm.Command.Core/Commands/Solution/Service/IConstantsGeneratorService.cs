using Greg.Xrm.Command.Commands.Solution.Model;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Solution.Service
{
	public interface IConstantsGeneratorService
	{
		Task<(int csFiles, int jsFiles)> GenerateAsync(
			IOrganizationServiceAsync2 crm,
			ConstantsOutputRequest request,
			CancellationToken cancellationToken);
	}
}
