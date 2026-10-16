using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Model
{
	public interface IConnectionReferenceRepository
	{
		/// <summary>
		/// Returns which of the given connection reference logical names actually
		/// exist in the current environment.
		/// </summary>
		Task<IReadOnlyCollection<string>> GetExistingLogicalNamesAsync(IOrganizationServiceAsync2 crm, IReadOnlyCollection<string> logicalNames);
	}
}
