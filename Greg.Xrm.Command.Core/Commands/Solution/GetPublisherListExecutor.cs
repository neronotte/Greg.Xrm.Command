using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Solution
{
	public class GetPublisherListExecutor : ICommandExecutor<GetPublisherListCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly string[] blackListPublisher = { "MicrosoftCorporation", "microsoftfirstparty" };

		public GetPublisherListExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
		}



		public async Task<CommandResult> ExecuteAsync(GetPublisherListCommand command, CancellationToken cancellationToken)
		{

			try
			{
				this.output.Write($"Connecting to the current dataverse environment...");
				var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
				this.output.WriteLine("Done", ConsoleColor.Green);

				var query = new QueryExpression("publisher");
				query.NoLock = true;
				// Add columns to query.ColumnSet
				query.ColumnSet.AddColumns(
					"friendlyname",
					"customizationprefix",
					"uniquename",
					"customizationoptionvalueprefix",
					"description",
					"createdby",
					"createdon");

				// Add conditions to query.Criteria
				query.Criteria.AddCondition("uniquename", ConditionOperator.NotIn, blackListPublisher);
				query.Criteria.AddCondition("isreadonly", ConditionOperator.Equal, false);

				var listPublisher = (await crm.RetrieveMultipleAsync(query)).Entities;


				this.output.WriteTable(listPublisher,
                    publisherListColumns(command.Verbose),
                    publisherListData(command.Verbose),
					(index, row) =>
					{
						if (index == 0)
							return ConsoleColor.Yellow;

						return null;
					}
				);


				return CommandResult.Success();
			}
			catch (Exception ex)
			{
				return CommandResult.Fail("Error while getting list of publishers: " + ex.Message, ex);
			}

		}

		private Func<string[]> publisherListColumns(bool verbose)
		{
			string[] columns = {
				"Unique name",
				"Friendly name",
				"Prefix"
			};

			if (verbose)
			{
				columns = columns.Concat(new[] {
					"Optionset prefix",
					"Created on",
					"Created by",
					"Description"
				}).ToArray();
			}

			return () => columns;
		}
		private Func<Entity, string[]> publisherListData(bool verbose)
		{
			return (user) =>
			{
				string[] values = {
					user.GetAttributeValue<string>("uniquename") ?? string.Empty,
					user.GetAttributeValue<string>("friendlyname") ?? string.Empty,
					user.GetAttributeValue<string>("customizationprefix") ?? string.Empty
				};

				if (verbose)
				{
					values = values.Concat(new[] {
						user.GetFormattedValue("customizationoptionvalueprefix") ?? string.Empty,
						user.GetAttributeValue<DateTime?>("createdon").GetValueOrDefault().ToLocalTime().ToString() ?? string.Empty,
						user.GetFormattedValue("createdby") ?? string.Empty,
						user.GetAttributeValue<string>("description") ?? string.Empty						
					}).ToArray();
				}

				return values;
			};
		}
	}
}
