using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateBooleanCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository)
	 : BaseCreateCommandExecutor<CreateBooleanCommand>(output, organizationServiceRepository, settingsRepository)
		, ICommandExecutor<CreateBooleanCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateBooleanCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateBooleanCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new BooleanAttributeMetadata();
			await SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			// Set extended properties
			attribute.OptionSet = new BooleanOptionSetMetadata(
				new OptionMetadata(new Label(command.TrueLabel, languageCode), 1),
				new OptionMetadata(new Label(command.FalseLabel, languageCode), 0)
			);

			return attribute;
		}
	}
}
