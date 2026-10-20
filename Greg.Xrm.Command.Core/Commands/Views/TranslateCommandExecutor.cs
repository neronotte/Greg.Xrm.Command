using Greg.Xrm.Command.Services.Views;
using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Views
{
	public class TranslateCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository organizationServiceRepository,
		IViewRetrieverService viewRetrieverService,
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
				var labels = ParseOptional(command.Labels);
				var description = ParseOptional(command.Description);
				var allLcids = labels.Keys.Concat(description.Keys).Distinct().ToArray();
				if (allLcids.Length == 0)
					return CommandResult.Fail("At least one translation must be provided.");

				await activeLanguageService.ValidateAsync(crm, allLcids, cancellationToken);

				var (result, view) = await viewRetrieverService.GetByNameAsync(
					crm,
					QueryType1.SavedQuery,
					command.Name!,
					command.TableName);
				if (view is null)
					return result;

				output.Write($"Updating translations for view '{view.name}'...");
				if (labels.Count > 0)
					await SetLabelsAsync(crm, view.Id, "name", labels, cancellationToken);
				if (description.Count > 0)
					await SetLabelsAsync(crm, view.Id, "description", description, cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				if (!command.NoPublish)
				{
					output.Write($"Publishing table '{view.returnedtypecode}'...");
					publishXmlBuilder.AddTable(view.returnedtypecode);
					var publishRequest = publishXmlBuilder.Build();
					if (publishRequest is not null)
						await crm.ExecuteAsync(publishRequest, cancellationToken);
					output.WriteLine("Done", ConsoleColor.Green);
				}

				var commandResult = CommandResult.Success();
				commandResult["ViewId"] = view.Id;
				commandResult["View"] = view.name ?? command.Name!;
				commandResult["Table"] = view.returnedtypecode;
				commandResult["TranslatedLanguages"] = allLcids.Length;
				commandResult["Published"] = !command.NoPublish;
				return commandResult;
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

		private static Task<OrganizationResponse> SetLabelsAsync(
			Microsoft.PowerPlatform.Dataverse.Client.IOrganizationServiceAsync2 organizationService,
			Guid viewId,
			string attributeName,
			IReadOnlyDictionary<int, string> translations,
			CancellationToken cancellationToken)
		{
			return organizationService.ExecuteAsync(
				new SetLocLabelsRequest
				{
					EntityMoniker = new EntityReference("savedquery", viewId),
					AttributeName = attributeName,
					Labels = translations
						.Select(translation => new LocalizedLabel(translation.Value, translation.Key))
						.ToArray(),
				},
				cancellationToken);
		}

		private IReadOnlyDictionary<int, string> ParseOptional(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? new Dictionary<int, string>()
				: translationParser.Parse(value);
		}
	}
}
