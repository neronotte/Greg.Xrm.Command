using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    internal class AttributeMetadataBuilderMemo : AttributeMetadataBuilderBase
    {
        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new MemoAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            // Set extended properties
            attribute.Format = StringFormat.TextArea;
            attribute.ImeMode = command.ImeMode;
            attribute.MaxLength = AttributeMetadataBuilderString.GetMaxLength(command.MaxLength);

            return Task.FromResult((AttributeMetadata)attribute);
        }
    }
}
