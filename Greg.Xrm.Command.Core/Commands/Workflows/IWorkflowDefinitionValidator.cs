using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Workflows
{
	/// <summary>
	/// Validates a flow definition before it is written to its target workflow,
	/// so that no malformed definition can reach the clientdata column of a flow
	/// that people actually use.
	/// </summary>
	public interface IWorkflowDefinitionValidator
	{
		/// <summary>
		/// Validates the given clientdata. Returns <c>null</c> when the definition is
		/// valid, or the failure to return to the caller.
		/// </summary>
		Task<CommandResult?> ValidateAsync(IOrganizationServiceAsync2 crm, string clientData, CancellationToken cancellationToken);
	}
}
