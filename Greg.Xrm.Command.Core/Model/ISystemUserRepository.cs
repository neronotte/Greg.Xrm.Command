using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	/// <summary>
	/// Options used to search system users.
	/// </summary>
	/// <param name="IncludeDisabled">If true, disabled users (including SYSTEM and INTEGRATION) are returned too.</param>
	/// <param name="IncludeApplicationUsers">If true, application users (service principals) are returned too.</param>
	/// <param name="Top">If specified, limits the number of returned users.</param>
	public sealed record SystemUserSearchOptions(bool IncludeDisabled = false, bool IncludeApplicationUsers = false, int? Top = null)
	{
		public static SystemUserSearchOptions Default { get; } = new();
	}

	public interface ISystemUserRepository
	{
		/// <summary>
		/// Returns the user with the given id, or null if not found.
		/// </summary>
		Task<SystemUser?> GetByIdAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Returns the user with the given domain name, or null if not found.
		/// </summary>
		Task<SystemUser?> GetByDomainNameAsync(IOrganizationServiceAsync2 crm, string domainName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Returns at most <paramref name="top"/> users whose domain name or primary email equals <paramref name="value"/>.
		/// </summary>
		Task<IReadOnlyList<SystemUser>> GetByDomainNameOrEmailAsync(IOrganizationServiceAsync2 crm, string value, int top, CancellationToken cancellationToken = default);

		/// <summary>
		/// Returns the users in the system.
		/// If <paramref name="query"/> is a guid, returns the user with that id (regardless of <paramref name="options"/> filters).
		/// Otherwise, if not empty, returns the users whose first name, last name or domain name contains the given text.
		/// </summary>
		Task<IReadOnlyList<SystemUser>> SearchAsync(IOrganizationServiceAsync2 crm, string? query, SystemUserSearchOptions? options = null, CancellationToken cancellationToken = default);
	}
}
