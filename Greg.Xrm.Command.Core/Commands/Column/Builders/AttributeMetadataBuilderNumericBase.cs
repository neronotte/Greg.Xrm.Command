using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
    internal class AttributeMetadataBuilderNumericBase : AttributeMetadataBuilderBase
    {
        protected enum Limit { Max, Min }

        public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
        {
            throw new NotImplementedException();
        }

        protected static int GetIntValue(double? doubleValue, Limit limit)
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
        protected static double GetDoubleValue(double? doubleValue, Limit limit)
        {
            var value = doubleValue == null ? (double?)null : Convert.ToDouble(doubleValue.Value);

            if (limit == Limit.Min)
            {
                if (value == null) return int.MinValue;
                if (value < Int64.MinValue)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The minimum value must be higher than -{Int64.MinValue} (Int64.MinValue)");
                }
                return value.Value;
            }
            else
            {
                if (value == null) return int.MaxValue;
                if (value > Int64.MaxValue)
                {
                    throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The maximum value must be lower than {Int64.MaxValue} (Int64.MaxValue)");
                }
                return value.Value;
            }
        }
    }
}
