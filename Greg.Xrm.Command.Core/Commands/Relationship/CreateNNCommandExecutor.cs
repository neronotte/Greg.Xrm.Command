using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Relationship
{
    public class CreateNNCommandExecutor : ICommandExecutor<CreateNNCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;

		public CreateNNCommandExecutor(IOutput output, IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}


		public async Task<CommandResult> ExecuteAsync(CreateNNCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			try
			{
				var defaultLanguageCode = await crm.GetDefaultLanguageCodeAsync();

				var currentSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(currentSolutionName))
				{
					currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (currentSolutionName == null)
					{
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.");
					}
				}



				output.WriteLine("Checking solution existence and retrieving publisher prefix");

				var query = new QueryExpression("solution");
				query.ColumnSet.AddColumns("ismanaged");
				query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
				var link = query.AddLink("publisher", "publisherid", "publisherid");
				link.Columns.AddColumns("customizationprefix");
				link.EntityAlias = "publisher";
				query.NoLock = true;
				query.TopCount = 1;


				var solutionList = (await crm.RetrieveMultipleAsync(query)).Entities;
				if (solutionList.Count == 0)
				{
					return CommandResult.Fail("Invalid solution name: " + currentSolutionName);
				}

				var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
				if (managed)
				{
					return CommandResult.Fail("The provided solution is managed. You must specify an unmanaged solution.");
				}

				var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
				if (string.IsNullOrWhiteSpace(publisherPrefix))
				{
					return CommandResult.Fail("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.");
				}

				ICreateNNStrategy strategy;
				if (command.Explicit)
				{
					strategy = new CreateNNExplicitStrategy(output, crm);
				}
				else
				{
					strategy = new CreateNNImplicitStrategy(output, crm);
				}


				var result = await strategy.CreateAsync(command, currentSolutionName, defaultLanguageCode, publisherPrefix);
				return result;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				return CommandResult.Fail(ex.Message, ex);
			}
		}
	}
}
