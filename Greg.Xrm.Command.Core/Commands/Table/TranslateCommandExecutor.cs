using Greg.Xrm.Command.Commands.WebResources.PushLogic;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Translations;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Table
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
				var singular = translationParser.Parse(command.Singular!);
				var plural = translationParser.Parse(command.Plural!);
				if (!singular.Keys.OrderBy(x => x).SequenceEqual(plural.Keys.OrderBy(x => x)))
					return CommandResult.Fail("The singular and plural translations must contain the same LCIDs.");

				var description = string.IsNullOrWhiteSpace(command.Description)
					? null
					: translationParser.Parse(command.Description);
				var allLcids = singular.Keys
					.Concat(description?.Keys ?? [])
					.Distinct();
				await activeLanguageService.ValidateAsync(crm, allLcids, cancellationToken);

				output.Write($"Retrieving metadata for table '{command.TableName}'...");
				var retrieveResponse = (RetrieveEntityResponse)await crm.ExecuteAsync(
					new RetrieveEntityRequest
					{
						LogicalName = command.TableName,
						EntityFilters = EntityFilters.Entity,
						RetrieveAsIfPublished = true,
					},
					cancellationToken);
				output.WriteLine("Done", ConsoleColor.Green);

				var table = retrieveResponse.EntityMetadata
					?? throw new CommandException(CommandException.XrmError, "Dataverse returned no table metadata.");
				table.DisplayName = CreateLabel(singular);
				table.DisplayCollectionName = CreateLabel(plural);
				if (description is not null)
					table.Description = CreateLabel(description);

				output.Write($"Updating table '{command.TableName}'...");
				await crm.ExecuteAsync(
					new UpdateEntityRequest
					{
						Entity = table,
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
				result["TranslatedLanguages"] = singular.Keys
					.Concat(description?.Keys ?? [])
					.Distinct()
					.Count();
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
