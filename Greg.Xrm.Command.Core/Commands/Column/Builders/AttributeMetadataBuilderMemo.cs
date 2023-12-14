using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    internal class AttributeMetadataBuilderMemo : AttributeMetadataBuilderBase
    {
        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new MemoAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            // Set extended properties
            //overridden of default values of commands in common with Text command
            attribute.Format = command.StringFormat == StringFormat.Text ? StringFormat.TextArea : command.StringFormat;
            attribute.FormatName = GetFormatName(command.MemoFormat);
            attribute.ImeMode = command.ImeMode == ImeMode.Disabled ? ImeMode.Auto : command.ImeMode;
            attribute.MaxLength = GetMaxLength(command.MaxLength);

            return Task.FromResult((AttributeMetadata)attribute);
        }


        private static MemoFormatName GetFormatName(MemoFormatName1 memoFormatName1)
        {
            return memoFormatName1 switch
            {
                MemoFormatName1.Text => MemoFormatName.Text,
                MemoFormatName1.Email => MemoFormatName.Email,
                MemoFormatName1.Json => MemoFormatName.Json,
                MemoFormatName1.RichText => MemoFormatName.RichText,
                MemoFormatName1.TextArea => MemoFormatName.TextArea,
                _ => throw new NotSupportedException($"MemoFormatName1 {memoFormatName1} is not supported."),
            };
        }
        internal int GetMaxLength(int? maxLength)
        {
            if (maxLength == null) return 2000;
            if (maxLength < MemoAttributeMetadata.MinSupportedLength || maxLength > MemoAttributeMetadata.MaxSupportedLength)
                throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max length must be between {MemoAttributeMetadata.MinSupportedLength} and {MemoAttributeMetadata.MaxSupportedLength} ");
            return maxLength.Value;
        }
    }
}
