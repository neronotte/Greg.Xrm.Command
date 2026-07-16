using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Greg.Xrm.Command.Commands.Data.Query
{
	public interface IQueryExecutor
	{
		Task<IReadOnlyCollection<Entity>> ExecuteQueryAsync(IOrganizationServiceAsync2 crm, CancellationToken cancellationToken);
	}
}
