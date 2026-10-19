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
				var translations = ParseOptional(command.Labels);
				var displayName = ParseOptional(command.DisplayName);
				var description = ParseOptional(command.Description);
				var allLcids = translations.Keys
					.Concat(displayName.Keys)
					.Concat(description.Keys)
					.Distinct()
					.ToArray();
				if (allLcids.Length == 0)
					return CommandResult.Fail("At least one translation must be provided.");
				await activeLanguageService.ValidateAsync(crm, allLcids, cancellationToken);

				OptionSetMetadata? globalOptionSet = null;
				if (!string.IsNullOrWhiteSpace(command.Name))
				{
					globalOptionSet = await RetrieveGlobalOptionSetAsync(crm, command.Name, cancellationToken);
					if (command.Value.HasValue)
						EnsureOptionExists(globalOptionSet.Options, command.Value.Value, $"option set '{command.Name}'");
				}
				else if (command.Value.HasValue)
				{
					var localOptions = await RetrieveLocalOptionsAsync(crm, command, cancellationToken);
					EnsureOptionExists(localOptions, command.Value.Value, $"'{command.TableName}.{command.ColumnName}'");
				}

				if (globalOptionSet is not null && (displayName.Count > 0 || description.Count > 0))
				{
					if (displayName.Count > 0)
						globalOptionSet.DisplayName = CreateLabel(displayName);
					if (description.Count > 0)
						globalOptionSet.Description = CreateLabel(description);

					output.Write($"Updating global option set '{command.Name}'...");
					await crm.ExecuteAsync(
						new UpdateOptionSetRequest
						{
							OptionSet = globalOptionSet,
							MergeLabels = true,
						},
						cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}

				if (command.Value.HasValue)
				{
					output.Write($"Updating option set value '{command.Value}'...");
					if ("statecode".Equals(command.ColumnName, StringComparison.OrdinalIgnoreCase))
					{
						await crm.ExecuteAsync(
							new UpdateStateValueRequest
							{
								EntityLogicalName = command.TableName,
								AttributeLogicalName = command.ColumnName,
								Value = command.Value.Value,
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
								Value = command.Value.Value,
								Label = CreateLabel(translations),
								MergeLabels = true,
							},
							cancellationToken);
					}
					output.WriteLine("Done", ConsoleColor.Green);
				}

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
				if (command.Value.HasValue)
					result["Value"] = command.Value.Value;
				result["TranslatedLanguages"] = allLcids.Length;
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

		private static async Task<OptionSetMetadata> RetrieveGlobalOptionSetAsync(
			Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 organizationService,
			string optionSetName,
			CancellationToken cancellationToken)
		{
			var response = (RetrieveOptionSetResponse)await organizationService.ExecuteAsync(
				new RetrieveOptionSetRequest { Name = optionSetName },
				cancellationToken);
			return response.OptionSetMetadata as OptionSetMetadata
				?? throw new CommandException(
					CommandException.XrmError,
					$"Dataverse returned no metadata for global option set '{optionSetName}'.");
		}

		private static async Task<OptionMetadataCollection> RetrieveLocalOptionsAsync(
			Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 organizationService,
			TranslateCommand command,
			CancellationToken cancellationToken)
		{
			var attributeResponse = (RetrieveAttributeResponse)await organizationService.ExecuteAsync(
				new RetrieveAttributeRequest
				{
					EntityLogicalName = command.TableName,
					LogicalName = command.ColumnName,
					RetrieveAsIfPublished = true,
				},
				cancellationToken);
			var attribute = attributeResponse.AttributeMetadata;
			return attribute is EnumAttributeMetadata enumMetadata
				? enumMetadata.OptionSet?.Options ?? []
				: [];
		}

		private static void EnsureOptionExists(
			IEnumerable<OptionMetadata> options,
			int value,
			string target)
		{
			if (!options.Any(option => option.Value == value))
				throw new CommandException(CommandException.CommandInvalidArgumentValue,
					$"Option value {value} does not exist in {target}.");
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
