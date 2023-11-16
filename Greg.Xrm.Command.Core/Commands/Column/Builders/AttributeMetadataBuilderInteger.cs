using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    internal class AttributeMetadataBuilderInteger : AttributeMetadataBuilderBase
    {
        private enum Limit { Max, Min }

        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            var attribute = new IntegerAttributeMetadata();
            SetCommonProperties(attribute, command, languageCode, publisherPrefix);

            attribute.MinValue = GetValue(command.MinValue, Limit.Min);
            attribute.MaxValue = GetValue(command.MaxValue, Limit.Max);
            attribute.Format = command.IntegerFormat;

            return Task.FromResult((AttributeMetadata)attribute);
        }

        private static int GetValue(double? doubleValue, Limit limit)
        {
            var value = doubleValue == null ? (int?)null : Convert.ToInt32(Math.Floor(doubleValue.Value));

            if (limit == Limit.Min)
            {
                if (value == null) return int.MinValue;
                if (value < int.MinValue)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The minimum value must be higher than -{int.MinValue} (int.MinValue)");
                }
                return value.Value;
            }
            else
            {
                if (value == null) return int.MaxValue;
                if (value > int.MaxValue)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The maximum value must be lower than {int.MaxValue} (int.MaxValue)");
                }
                return value.Value;
            }
        }
    }
}
