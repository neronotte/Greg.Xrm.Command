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
				var translations = ParseOptional(command.Labels);
				var description = ParseOptional(command.Description);
				var trueLabel = ParseOptional(command.TrueLabel);
				var falseLabel = ParseOptional(command.FalseLabel);
				var allLcids = translations.Keys
					.Concat(description.Keys)
					.Concat(trueLabel.Keys)
					.Concat(falseLabel.Keys)
					.Distinct();
				if (!allLcids.Any())
					return CommandResult.Fail("At least one translation must be provided.");
				await activeLanguageService.ValidateAsync(crm, allLcids, cancellationToken);

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
				if (translations.Count > 0)
					attribute.DisplayName = CreateLabel(translations);
				if (description.Count > 0)
					attribute.Description = CreateLabel(description);
				if (trueLabel.Count > 0 || falseLabel.Count > 0)
				{
					if (attribute is not BooleanAttributeMetadata booleanAttribute ||
						booleanAttribute.OptionSet is null)
					{
						return CommandResult.Fail(
							"True and false labels can only be used with Boolean columns.");
					}

					if (trueLabel.Count > 0)
						booleanAttribute.OptionSet.TrueOption.Label = CreateLabel(trueLabel);
					if (falseLabel.Count > 0)
						booleanAttribute.OptionSet.FalseOption.Label = CreateLabel(falseLabel);
				}

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
				result["TranslatedLanguages"] = allLcids.Count();
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

		private IReadOnlyDictionary<int, string> ParseOptional(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? new Dictionary<int, string>()
				: translationParser.Parse(value);
		}
	}
}
