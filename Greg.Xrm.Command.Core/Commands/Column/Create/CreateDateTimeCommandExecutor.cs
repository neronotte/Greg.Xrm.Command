using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateDateTimeCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository)
	 : BaseCreateCommandExecutor<CreateDateTimeCommand>(output, organizationServiceRepository, settingsRepository)
		, ICommandExecutor<CreateDateTimeCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateDateTimeCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateDateTimeCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new DateTimeAttributeMetadata();
			await SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.DateTimeBehavior = GetBehavior(command.DateTimeBehavior);
			attribute.Format = command.DateTimeFormat;
			attribute.ImeMode = command.ImeMode;

			return attribute;
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
