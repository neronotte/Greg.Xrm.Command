using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column
{
    internal class AttributeMetadataBuilderInteger : AttributeMetadataBuilderBase
    {
        private enum Limit { Max, Min }
        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new IntegerAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            attribute.MinValue = GetValue(command.IntegerMinValue, Limit.Min);
            attribute.MaxValue = GetValue(command.IntegerMaxValue, Limit.Max);

            attribute.Format = command.IntegerFormat;

            return Task.FromResult((AttributeMetadata)attribute);
        }

        private int GetValue(int? value, Limit limit)
        {
            if (limit == Limit.Min)
            {
                if (value == null) return int.MinValue;
                if (value < int.MinValue)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The minimum value must be higher than -2147483648 (int.MinValue)");
                }
                return value.Value;
            }
            else
            {
                if (value == null) return int.MaxValue;
                if (value > int.MaxValue)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The maximum value must be lower than 2147483647 (int.MaxValue)");
                }
                return value.Value;
            }
        }
    }
}
