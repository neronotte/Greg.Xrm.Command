using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Connection
{
	public interface IOrganizationServiceRepository
	{
		string GetTokenCachePath();

		Task<ConnectionSetting> GetAllConnectionDefinitionsAsync();

		Task<IOrganizationServiceAsync2> GetCurrentConnectionAsync();
		Task<IOrganizationServiceAsync2> GetConnectionByName(string connectionName);

		Task<string?> GetEnvironmentFromConnectioStringAsync(string connectionName);

		Task SetConnectionAsync(string name, string connectionString);
		Task DeleteConnectionAsync(string name);

		Task SetDefaultAsync(string name);
		Task SetDefaultSolutionAsync(string uniqueName);
		Task<string?> GetCurrentDefaultSolutionAsync();
		Task RenameConnectionAsync(string oldName, string newName);
	}
}
