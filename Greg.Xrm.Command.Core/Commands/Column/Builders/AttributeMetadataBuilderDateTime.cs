using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
	internal class AttributeMetadataBuilderDateTime : AttributeMetadataBuilderBase
	{
		public override Task<AttributeMetadata> CreateFromAsync(IOrganizationServiceAsync2 crm, CreateCommand command, int languageCode, string publisherPrefix, int customizationOptionValuePrefix)
		{
			var attribute = new DateTimeAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.DateTimeBehavior = GetBehavior(command.DateTimeBehavior);
			attribute.Format = command.DateTimeFormat;
			attribute.ImeMode = command.ImeMode;

			return Task.FromResult((AttributeMetadata)attribute);
		}

		private static DateTimeBehavior GetBehavior(DateTimeBehavior1 dateTimeBehavior)
		{
			return dateTimeBehavior switch
			{
				DateTimeBehavior1.UserLocal => DateTimeBehavior.UserLocal,
				DateTimeBehavior1.TimeZoneIndependent => DateTimeBehavior.TimeZoneIndependent,
				DateTimeBehavior1.DateOnly => DateTimeBehavior.DateOnly,
				_ => throw new NotSupportedException($"DateTimeBehavior {dateTimeBehavior} is not supported."),
			};
		}
	}
}
