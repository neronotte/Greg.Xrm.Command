using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Services
{
    public interface IAttributeDeletionService
	{
		Task DeleteAttributeAsync(IOrganizationServiceAsync2 crm, AttributeMetadata attribute, DependencyList dependencies, bool? simulation = false);
	}
}
