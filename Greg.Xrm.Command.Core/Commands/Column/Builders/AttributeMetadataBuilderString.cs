using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{

    public class AttributeMetadataBuilderString : AttributeMetadataBuilderBase
    {
        public override Task<AttributeMetadata> CreateFromAsync(
            IOrganizationServiceAsync2 crm,
            CreateCommand command,
            int languageCode,
            string publisherPrefix,
            int customizationOptionValuePrefix)
        {
            var attribute = new StringAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            attribute.MaxLength = GetMaxLength(command.MaxLength);
            attribute.Format = command.StringFormat;
            attribute.AutoNumberFormat = command.AutoNumber;

            return Task.FromResult((AttributeMetadata)attribute);
        }



        public static int GetMaxLength(int? maxLength)
        {
            if (maxLength == null) return 100;
            if (maxLength <= 0) throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max length must be a positive number");
            return maxLength.Value;
        }
    }
}
