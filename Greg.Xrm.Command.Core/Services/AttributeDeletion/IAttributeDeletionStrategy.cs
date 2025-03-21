using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services.AttributeDeletion
{
    public interface IAttributeDeletionStrategy
	{
        Task HandleAsync(
            IOrganizationServiceAsync2 crm,
            AttributeMetadata attribute,
            DependencyList dependencies);
    }
}
