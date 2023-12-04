using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using System.Text;

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


		public async Task ExecuteAsync(CreateNNCommand command, CancellationToken cancellationToken)
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
						output.WriteLine("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.", ConsoleColor.Red);
						return;
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
					output.WriteLine("Invalid solution name: ", ConsoleColor.Red).WriteLine(currentSolutionName, ConsoleColor.Red);
					return;
				}

				var managed = solutionList[0].GetAttributeValue<bool>("ismanaged");
				if (managed)
				{
					output.WriteLine("The provided solution is managed. You must specify an unmanaged solution.", ConsoleColor.Red);
					return;
				}

				var publisherPrefix = solutionList[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix").Value as string;
				if (string.IsNullOrWhiteSpace(publisherPrefix))
				{
					output.WriteLine("Unable to retrieve the publisher prefix. Please report a bug to the project GitHub page.", ConsoleColor.Red);
					return;
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


				await strategy.CreateAsync(command, currentSolutionName, defaultLanguageCode, publisherPrefix);



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
	}
}
