using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateDecimalCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
	 : BaseCreateCommandExecutor<CreateDecimalCommand>(output, organizationServiceRepository)
		, ICommandExecutor<CreateDecimalCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateDecimalCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateDecimalCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new DecimalAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			// Set extended properties
			attribute.MinValue = Convert.ToDecimal(GetDoubleValue(command.MinValue, Limit.Min));
			attribute.MaxValue = Convert.ToDecimal(GetDoubleValue(command.MaxValue, Limit.Max));

			attribute.Precision = command.Precision; // 2
			attribute.ImeMode = command.ImeMode; // ImeMode.Disabled

			return Task.FromResult((AttributeMetadata)attribute);
		}
	}
}
