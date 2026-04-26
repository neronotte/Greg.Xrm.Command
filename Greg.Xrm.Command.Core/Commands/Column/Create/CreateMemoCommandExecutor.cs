using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	public class CreateMemoCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository)
	 : BaseCreateCommandExecutor<CreateMemoCommand>(output, organizationServiceRepository, settingsRepository)
		, ICommandExecutor<CreateMemoCommand>
	{
		public async Task<CommandResult> ExecuteAsync(CreateMemoCommand command, CancellationToken cancellationToken)
		{
			return await base.ExecuteAsync(command, CreateFromAsync, cancellationToken);
		}


		public async Task<AttributeMetadata> CreateFromAsync(
			IOrganizationServiceAsync2 crm,
			CreateMemoCommand command,
			int languageCode,
			string publisherPrefix,
			int customizationOptionValuePrefix)
		{
			var attribute = new MemoAttributeMetadata();
			await SetCommonProperties(attribute, command, languageCode, publisherPrefix);

			// Set extended properties
			//overridden of default values of commands in common with Text command
			attribute.Format = GetFormat(command.MemoFormat);
			attribute.FormatName = GetFormatName(command.MemoFormat);
			attribute.ImeMode = command.ImeMode == ImeMode.Disabled ? ImeMode.Auto : command.ImeMode;
			attribute.MaxLength = GetMaxLength(command.MaxLength);

			return attribute;
		}

		private static StringFormat GetFormat(MemoFormatName1 memoFormatName1)
		{
			return memoFormatName1 switch
			{
				MemoFormatName1.Text => StringFormat.Text,
				MemoFormatName1.Email => StringFormat.Email,
				MemoFormatName1.Json => StringFormat.Json,
				MemoFormatName1.RichText => StringFormat.RichText,
				MemoFormatName1.TextArea => StringFormat.TextArea,
				_ => throw new NotSupportedException($"MemoFormat {memoFormatName1} is not supported."),
			};
		}


		private static MemoFormatName GetFormatName(MemoFormatName1 memoFormatName1)
		{
			return memoFormatName1 switch
			{
				MemoFormatName1.Text => MemoFormatName.Text,
				MemoFormatName1.Email => MemoFormatName.Email,
				MemoFormatName1.Json => MemoFormatName.Json,
				MemoFormatName1.RichText => MemoFormatName.RichText,
				MemoFormatName1.TextArea => MemoFormatName.TextArea,
				_ => throw new NotSupportedException($"MemoFormat {memoFormatName1} is not supported."),
			};
		}
		private static int GetMaxLength(int? maxLength)
		{
			if (maxLength == null) return 2000;
			if (maxLength < MemoAttributeMetadata.MinSupportedLength || maxLength > MemoAttributeMetadata.MaxSupportedLength)
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The max length must be between {MemoAttributeMetadata.MinSupportedLength} and {MemoAttributeMetadata.MaxSupportedLength} ");
			return maxLength.Value;
		}
	}
}
