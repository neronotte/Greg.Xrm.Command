using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateFileCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
	 : BaseCreateCommandExecutor<CreateFileCommand>(output, organizationServiceRepository)
		, ICommandExecutor<CreateFileCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateFileCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateFileCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new FileAttributeMetadata();
			SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			attribute.MaxSizeInKB = GetMaxSizeKb(command.MaxSizeInKB);

			return Task.FromResult((AttributeMetadata)attribute);
		}

		public static int GetMaxSizeKb(int? maxSizeKb)
		{
			if (maxSizeKb == null) return 32 * 1024;
			if (maxSizeKb <= 0) throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max size in KB must be a positive number");
			if (maxSizeKb > 10485760) throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max size in KB must be less then 10485760");
			return maxSizeKb.Value;
		}
	}
}
