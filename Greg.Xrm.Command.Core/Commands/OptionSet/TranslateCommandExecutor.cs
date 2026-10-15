using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.OptionSet
{
	public class TranslateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		ITranslationParser translationParser,
		IActiveLanguageService activeLanguageService,
		IPublishXmlBuilder publishXmlBuilder)
		: ICommandExecutor<TranslateCommand>
	{
		public async Task<CommandResult> ExecuteAsync(TranslateCommand command, CancellationToken cancellationToken)
		{
			output.Write("Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var translations = translationParser.Parse(command.Labels!);
				await activeLanguageService.ValidateAsync(crm, translations.Keys, cancellationToken);
				await EnsureOptionExistsAsync(crm, command, cancellationToken);

				output.Write($"Updating option set value '{command.Value}'...");
				if ("statecode".Equals(command.ColumnName, StringComparison.OrdinalIgnoreCase))
				{
					await crm.ExecuteAsync(
						new UpdateStateValueRequest
						{
							EntityLogicalName = command.TableName,
							AttributeLogicalName = command.ColumnName,
							Value = command.Value,
							Label = CreateLabel(translations),
							MergeLabels = true,
						},
						cancellationToken);
				}
				else
				{
					await crm.ExecuteAsync(
						new UpdateOptionValueRequest
						{
							OptionSetName = string.IsNullOrWhiteSpace(command.Name) ? null : command.Name,
							EntityLogicalName = string.IsNullOrWhiteSpace(command.Name) ? command.TableName : null,
							AttributeLogicalName = string.IsNullOrWhiteSpace(command.Name) ? command.ColumnName : null,
							Value = command.Value,
							Label = CreateLabel(translations),
							MergeLabels = true,
						},
						cancellationToken);
				}
				output.WriteLine("Done", ConsoleColor.Green);

				if (!command.NoPublish)
				{
					output.Write("Publishing metadata changes...");
					if (string.IsNullOrWhiteSpace(command.Name))
						publishXmlBuilder.AddTable(command.TableName);
					else
						publishXmlBuilder.AddGlobalOptionSet(command.Name);

					var publishRequest = publishXmlBuilder.Build();
					if (publishRequest is not null)
						await crm.ExecuteAsync(publishRequest, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}

				var result = CommandResult.Success();
				result["Value"] = command.Value;
				result["TranslatedLanguages"] = translations.Count;
				result["Published"] = !command.NoPublish;
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
			catch (Exception ex)
			{
				output.WriteLine("Failed", ConsoleColor.Red);
				return CommandResult.Fail(ex.Message, ex);
			}
		}

		private static async Task EnsureOptionExistsAsync(
			Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 organizationService,
			TranslateCommand command,
			CancellationToken cancellationToken)
		{
			if (!string.IsNullOrWhiteSpace(command.Name))
			{
				var response = (RetrieveOptionSetResponse)await organizationService.ExecuteAsync(
					new RetrieveOptionSetRequest { Name = command.Name },
					cancellationToken);
				var optionSet = response.OptionSetMetadata as OptionSetMetadata;
				var globalOptions = optionSet?.Options ?? [];
				if (!globalOptions.Any(option => option.Value == command.Value))
					throw new CommandException(CommandException.CommandInvalidArgumentValue,
						$"Option value {command.Value} does not exist in option set '{command.Name}'.");
				return;
			}

			var attributeResponse = (RetrieveAttributeResponse)await organizationService.ExecuteAsync(
				new RetrieveAttributeRequest
				{
					EntityLogicalName = command.TableName,
					LogicalName = command.ColumnName,
					RetrieveAsIfPublished = true,
				},
				cancellationToken);
			var attribute = attributeResponse.AttributeMetadata;
			var localOptions = attribute is EnumAttributeMetadata enumMetadata
				? enumMetadata.OptionSet?.Options ?? []
				: [];
			if (!localOptions.Any(option => option.Value == command.Value))
				throw new CommandException(CommandException.CommandInvalidArgumentValue,
					$"Option value {command.Value} does not exist in '{command.TableName}.{command.ColumnName}'.");
		}

		private static Label CreateLabel(IReadOnlyDictionary<int, string> translations)
		{
			var label = new Label();
			foreach (var translation in translations)
				label.LocalizedLabels.Add(new LocalizedLabel(translation.Value, translation.Key));

			return label;
		}
	}
}
