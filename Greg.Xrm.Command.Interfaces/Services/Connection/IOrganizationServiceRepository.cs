using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Services.Connection
{
	public interface IOrganizationServiceRepository
	{
		/// <summary>
		/// Returns the file path used to persist the MSAL token cache on disk.
		/// </summary>
		string GetTokenCachePath();

		/// <summary>
		/// Sets a temporary environment override for the current command invocation.
		/// Subsequent calls to <see cref="GetCurrentConnectionAsync"/> and
		/// <see cref="GetCurrentConnectionNameAsync"/> will resolve against
		/// <paramref name="nameOrUrl"/> instead of the default profile.
		/// Matching is attempted first by profile name (case-insensitive), then by
		/// environment URL (trailing slash and case are normalised).
		/// </summary>
		/// <param name="nameOrUrl">
		/// The name of an existing authentication profile, or the root URL of the
		/// Dataverse environment (e.g. <c>https://myorg.crm4.dynamics.com</c>).
		/// </param>
		void SetEnvironmentOverride(string nameOrUrl);

		/// <summary>
		/// Returns the resolved profile name of the active environment override, or
		/// <c>null</c> if no override has been set via <see cref="SetEnvironmentOverride"/>.
		/// </summary>
		Task<string?> GetCurrentEnvironmentOverrideNameAsync();

		/// <summary>
		/// Returns the full <see cref="ConnectionSetting"/> object containing all stored
		/// authentication profiles and their associated metadata.
		/// </summary>
		Task<ConnectionSetting> GetAllConnectionDefinitionsAsync();

		/// <summary>
		/// Opens and returns an authenticated <see cref="IOrganizationServiceAsync2"/> for
		/// the currently active environment (respecting any active override).
		/// </summary>
		Task<IOrganizationServiceAsync2> GetCurrentConnectionAsync();

		/// <summary>
		/// Opens and returns an authenticated <see cref="IOrganizationServiceAsync2"/> for
		/// the authentication profile identified by <paramref name="connectionName"/>.
		/// </summary>
		/// <param name="connectionName">The name of an existing authentication profile.</param>
		Task<IOrganizationServiceAsync2> GetConnectionByName(string connectionName);

		/// <summary>
		/// Returns the Dataverse environment URL associated with the given authentication
		/// profile, or <c>null</c> if the profile does not contain a recognisable URL token.
		/// </summary>
		/// <param name="connectionName">The name of an existing authentication profile.</param>
		Task<string?> GetEnvironmentFromConnectioStringAsync(string connectionName);

		/// <summary>
		/// Returns the name of the currently active authentication profile, resolving in
		/// priority order: environment override → project-level default → global default.
		/// </summary>
		Task<string> GetCurrentConnectionNameAsync();

		/// <summary>
		/// Creates or updates the authentication profile identified by <paramref name="name"/>
		/// with the supplied connection string.
		/// </summary>
		/// <param name="name">The display name for the profile.</param>
		/// <param name="connectionString">The Dataverse connection string (will be encrypted at rest).</param>
		Task SetConnectionAsync(string name, string connectionString);

		/// <summary>
		/// Permanently removes the authentication profile identified by <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the profile to delete.</param>
		Task DeleteConnectionAsync(string name);

		/// <summary>
		/// Sets the global default authentication profile to <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the profile to make the default.</param>
		Task SetDefaultAsync(string name);

		/// <summary>
		/// Associates a default solution unique name with the currently active authentication
		/// profile so that solution-scoped commands do not require an explicit <c>--solution</c>
		/// argument.
		/// </summary>
		/// <param name="uniqueName">The unique name of the Dataverse solution.</param>
		Task SetDefaultSolutionAsync(string uniqueName);

		/// <summary>
		/// Returns the default solution unique name associated with the currently active
		/// authentication profile, or <c>null</c> if none has been set.
		/// </summary>
		Task<string?> GetCurrentDefaultSolutionAsync();

		/// <summary>
		/// Renames an existing authentication profile from <paramref name="oldName"/> to
		/// <paramref name="newName"/>, preserving the connection string and all associated
		/// metadata.
		/// </summary>
		/// <param name="oldName">The current name of the profile.</param>
		/// <param name="newName">The new name for the profile.</param>
		Task RenameConnectionAsync(string oldName, string newName);
	}
}
