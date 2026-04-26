using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateImageCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository)
	 : BaseCreateCommandExecutor<CreateImageCommand>(output, organizationServiceRepository, settingsRepository)
		, ICommandExecutor<CreateImageCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateImageCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateImageCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new ImageAttributeMetadata();
			await SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.MaxSizeInKB = GetMaxSizeKb(command.MaxSizeInKB);

			// true if the value is not passed
			// or if the "store thumbnail" is set to false
			attribute.CanStoreFullImage = !command.StoreThumbnail.HasValue || !command.StoreThumbnail.Value;

			return attribute;
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
