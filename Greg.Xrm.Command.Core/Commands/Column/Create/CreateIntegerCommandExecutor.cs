using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateIntegerCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
	 : BaseCreateCommandExecutor<CreateIntegerCommand>(output, organizationServiceRepository)
		, ICommandExecutor<CreateIntegerCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateIntegerCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateIntegerCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new IntegerAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.MinValue = GetIntValue(command.MinValue, Limit.Min);
			attribute.MaxValue = GetIntValue(command.MaxValue, Limit.Max);
			attribute.Format = command.IntegerFormat;

			return Task.FromResult((AttributeMetadata)attribute);
		}
	}
}
