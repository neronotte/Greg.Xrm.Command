using Greg.Xrm.Command.Commands.Column.Builders;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Column
{
    public class CreateCommandExecutor : ICommandExecutor<CreateCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly IAttributeMetadataBuilderFactory attributeMetadataBuilderFactory;

		public CreateCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			IAttributeMetadataBuilderFactory attributeMetadataBuilderFactory)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.attributeMetadataBuilderFactory = attributeMetadataBuilderFactory;
		}


		public async Task ExecuteAsync(CreateCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);



			try
			{
				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();


				var (publisherPrefix, currentSolutionName, customizationOptionValuePrefix) = await CheckSolutionAndReturnPublisherPrefixAsync(crm, command.SolutionName);
				if (publisherPrefix == null) return;
				if (currentSolutionName == null) return;
				if (customizationOptionValuePrefix == null) return;


				var builder = attributeMetadataBuilderFactory.CreateFor(command.AttributeType);
				var attribute = await builder.CreateFromAsync(crm, command, defaultLanguageCode, publisherPrefix, customizationOptionValuePrefix.Value);


				output.Write($"Creating attribute {attribute.SchemaName}...");
				var request = new CreateAttributeRequest();
				request.SolutionUniqueName = currentSolutionName;
				request.EntityName = command.EntityName;
				request.Attribute = attribute;

				var response = (CreateAttributeResponse)await crm.ExecuteAsync(request, cancellationToken);

				output.WriteLine("Done", ConsoleColor.Green)
					.Write("  Column ID: ")
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
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					output.WriteLine("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.", ConsoleColor.Red);
					return (null, null, null);
				}
			}


			output.WriteLine("Checking solution existence and retrieving publisher prefix");

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


			var customizationOptionValuePrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationoptionvalueprefix").Value as int?;
			if (customizationOptionValuePrefix == null)
			{
				output.WriteLine("Unable to retrieve the optionset prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
				return (null, null, null);
			}

			return (publisherPrefix, currentSolutionName, customizationOptionValuePrefix);
		}
	}
}
