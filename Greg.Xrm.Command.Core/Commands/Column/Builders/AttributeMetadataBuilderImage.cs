using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Builders
{
	public class AttributeMetadataBuilderImage : AttributeMetadataBuilderBase
	{
		public override Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new ImageAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.MaxSizeInKB = GetMaxSizeKb(command.MaxSizeInKB);

			// true if the value is not passed
			// or if the "store thumbnail" is set to false
			attribute.CanStoreFullImage = !command.StoreThumbnail.HasValue || !command.StoreThumbnail.Value;

			return Task.FromResult((AttributeMetadata)attribute);
		}



		public static int GetMaxSizeKb(int? maxSizeKb)
		{
			if (maxSizeKb == null) return 10 * 1024;
			if (maxSizeKb <= 0) throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max size in KB must be a positive number");
			if (maxSizeKb > 30720) throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max size in KB must be less then 30720");
			return maxSizeKb.Value;
		}
	}
}
