using Greg.Xrm.Command.Commands.Create.Column;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Greg.Xrm.Command.Services.Settings;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Create
{
	public class CreateColumnCommandExecutor : ICommandExecutor<CreateColumnCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISettingsRepository settingsRepository;
		private readonly IAttributeMetadataBuilderFactory attributeMetadataBuilderFactory;

		public CreateColumnCommandExecutor(
			IOutput output, 
			IOrganizationServiceRepository organizationServiceRepository,
			ISettingsRepository settingsRepository,
			IAttributeMetadataBuilderFactory attributeMetadataBuilderFactory)
        {
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.settingsRepository = settingsRepository;
			this.attributeMetadataBuilderFactory = attributeMetadataBuilderFactory;
		}


        public async Task ExecuteAsync(CreateColumnCommand command, CancellationToken cancellationToken)
		{
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();

			int defaultLanguageCode = 1033;

			try
			{
				var (publisherPrefix, currentSolutionName, customizationOptionValuePrefix) = await CheckSolutionAndReturnPublisherPrefixAsync(crm, command.SolutionName);
				if (publisherPrefix == null) return;
				if (currentSolutionName == null) return;
				if (customizationOptionValuePrefix == null) return;


				if (command.AttributeType != AttributeTypeCode.String)
					throw new CommandException(CommandException.CommandInvalidArgumentValue, $"The attribute type '{command.AttributeType}' is not supported yet");

				var builder = this.attributeMetadataBuilderFactory.CreateFor(command.AttributeType);
				var attribute = builder.CreateFrom(command, defaultLanguageCode, publisherPrefix, customizationOptionValuePrefix.Value);


				this.output.Write($"Creating attribute {attribute.SchemaName}...");
				var request = new CreateAttributeRequest();
				request.SolutionUniqueName = currentSolutionName;
				request.EntityName = command.EntityName;
				request.Attribute = attribute;

				var response = (CreateAttributeResponse)(await crm.ExecuteAsync(request, cancellationToken));

				this.output.WriteLine("Done", ConsoleColor.Green)
					.Write("  Attribute Id:")
					.WriteLine(response.AttributeId.ToString(), ConsoleColor.Yellow);
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				output.WriteLine()
					.Write("Error: ", ConsoleColor.Red)
					.WriteLine(ex.Message, ConsoleColor.Red);

				if (ex.InnerException != null)
				{
					output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
				}
			}
		}








		private async Task<(string?, string?, int?)> CheckSolutionAndReturnPublisherPrefixAsync(IOrganizationServiceAsync2 crm, string? currentSolutionName)
		{
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await this.settingsRepository.GetAsync<string>("currentSolutionName");
				if (currentSolutionName == null)
				{
					output.WriteLine("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.", ConsoleColor.Red);
					return (null, null, null);
				}
			}


			this.output.WriteLine("Checking solution existence and retrieving publisher prefix");

			var query = new QueryExpression("solution");
			query.ColumnSet.AddColumns("ismanaged");
			query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
			var link = query.AddLink("publisher", "publisherid", "publisherid");
			link.Columns.AddColumns("customizationprefix", "customizationoptionvalueprefix");
			link.EntityAlias = "publisher";
			query.NoLock = true;
			query.TopCount = 1;


			var solutionList = (await crm.RetrieveMultipleAsync(query)).Entities;
			if (solutionList.Count == 0)
			{
				output.WriteLine("Invalid solution name: ", ConsoleColor.Red).WriteLine(currentSolutionName, ConsoleColor.Red);
				return (null, null, null);
			}

			var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
			if (managed)
			{
				output.WriteLine("The provided solution is managed. You must specify an unmanaged solution.", ConsoleColor.Red);
				return (null, null, null);
			}

			var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
			if (string.IsNullOrWhiteSpace(publisherPrefix))
			{
				output.WriteLine("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
				return (null, null, null);
			}


			var customizationOptionValuePrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as int?;
			if (customizationOptionValuePrefix == null)
			{
				output.WriteLine("Unable to retrieve the optionset prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
				return (null, null, null);
			}

			return (publisherPrefix, currentSolutionName, customizationOptionValuePrefix);
		}
	}
}
