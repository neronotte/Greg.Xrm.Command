using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
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

				output.Write($"Retrieving metadata for column '{command.TableName}.{command.ColumnName}'...");
				var retrieveResponse = (RetrieveAttributeResponse)await crm.ExecuteAsync(
					new RetrieveAttributeRequest
					{
						EntityLogicalName = command.TableName,
						LogicalName = command.ColumnName,
						RetrieveAsIfPublished = true,
					},
					cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				var attribute = retrieveResponse.AttributeMetadata
					?? throw new CommandException(CommandException.XrmError, "Dataverse returned no column metadata.");
				attribute.DisplayName = CreateLabel(translations);

				output.Write($"Updating column '{command.TableName}.{command.ColumnName}'...");
				await crm.ExecuteAsync(
					new UpdateAttributeRequest
					{
						EntityName = command.TableName,
						Attribute = attribute,
						MergeLabels = true,
					},
					cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				if (!command.NoPublish)
				{
					output.Write("Publishing metadata changes...");
					publishXmlBuilder.AddTable(command.TableName!);
					var publishRequest = publishXmlBuilder.Build();
					if (publishRequest is not null)
						await crm.ExecuteAsync(publishRequest, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}

				var result = CommandResult.Success();
				result["Table"] = command.TableName!;
				result["Column"] = command.ColumnName!;
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

		private static Label CreateLabel(IReadOnlyDictionary<int, string> translations)
		{
			var label = new Label();
			foreach (var translation in translations)
				label.LocalizedLabels.Add(new LocalizedLabel(translation.Value, translation.Key));

			return label;
		}
	}
}
