using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    public interface IAttributeMetadataBuilder
    {
        Task<AttributeMetadata> CreateFromAsync(
            IOrganizationServiceAsync2 crm,
            CreateCommand command,
            int languageCode,
            string publisherPrefix,
            int customizationOptionValuePrefix);
    }
}
