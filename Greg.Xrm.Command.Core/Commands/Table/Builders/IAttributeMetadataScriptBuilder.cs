using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public interface IAttributeMetadataScriptBuilder
    {

        string GetColumnScript(AttributeMetadata attributeMetadata); //, ScriptCommand command
        /*
         
            IOrganizationServiceAsync2 crm,
            CreateCommand command,
            int languageCode,
            string publisherPrefix,
            int customizationOptionValuePrefix
         */
    }
}
