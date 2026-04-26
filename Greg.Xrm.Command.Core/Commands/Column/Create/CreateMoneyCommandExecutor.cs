using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateMoneyCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository)
	 : BaseCreateCommandExecutor<CreateMoneyCommand>(output, organizationServiceRepository, settingsRepository)
		, ICommandExecutor<CreateMoneyCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateMoneyCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateMoneyCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new MoneyAttributeMetadata();
			await SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			// Set extended properties
			attribute.MinValue = GetDoubleValue(command.MinValue, Limit.Min);
			attribute.MaxValue = GetDoubleValue(command.MaxValue, Limit.Max);

			attribute.Precision = command.Precision; //1
			attribute.PrecisionSource = command.PrecisionSource; // default 2
			attribute.ImeMode = command.ImeMode; // ImeMode.Disabled

			if (attribute.PrecisionSource == 0 && attribute.Precision is null)
			{
				throw new CommandException(CommandException.CommandRequiredArgumentNotProvided, $"The attribute 'Precision' must be specified when PrecisionSource = 0");
			}


			return attribute;
		}
	}
}
