using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateStringCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository)
	 : BaseCreateCommandExecutor<CreateStringCommand>(output, organizationServiceRepository, settingsRepository)
		, ICommandExecutor<CreateStringCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateStringCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateStringCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new StringAttributeMetadata();
			await SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.MaxLength = GetMaxLength(command.MaxLength);
			attribute.Format = command.StringFormat;
			attribute.AutoNumberFormat = command.AutoNumber;

			return attribute;
		}



		public static int GetMaxLength(int? maxLength)
		{
			if (maxLength == null) return 100;
			if (maxLength <= 0) throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max length must be a positive number");
			return maxLength.Value;
		}
	}
}
