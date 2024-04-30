using Greg.Xrm.Command.Commands.Table.Migration;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Table
{
    public class TablePrintMermaidCommandExecutor : ICommandExecutor<TablePrintMermaidCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly TableGraphBuilder tableGraphBuilder;

		public TablePrintMermaidCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			TableGraphBuilder tableGraphBuilder)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.tableGraphBuilder = tableGraphBuilder;
		}


		public async Task<CommandResult> ExecuteAsync(TablePrintMermaidCommand command, CancellationToken cancellationToken)
		{
			output.Write($"Connecting to the current dataverse environment...");
			var crm = await organizationServiceRepository.GetCurrentConnectionAsync();
			output.WriteLine("Done", ConsoleColor.Green);


			var currentSolutionName = command.SolutionName;
			if (string.IsNullOrWhiteSpace(currentSolutionName))
			{
				currentSolutionName = await organizationServiceRepository.GetCurrentDefaultSolutionAsync();
				if (currentSolutionName == null)
				{
					return CommandResult.Fail("No solution name provided and no current solution name found in the settings. Please provide a solution name or set a current solution name in the settings.");
				}
			}






			output.Write($"Retrieving tables from solution '{currentSolutionName}'...");
			var query = new QueryExpression("solutioncomponent");
			query.ColumnSet.AddColumns("objectid");
			query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, 1);
			var solutionLink = query.AddLink("solution", "solutionid", "solutionid");
			solutionLink.LinkCriteria.AddCondition("uniquename", ConditionOperator.Equal, currentSolutionName);
			query.NoLock = true;

			var tableIds = (await crm.RetrieveMultipleAsync(query))
				.Entities
				.Select(x => x.GetAttributeValue<Guid>("objectid"))
				.Distinct()
				.ToList();

			output.WriteLine("Done", ConsoleColor.Green);

			output.WriteLine($"Found {tableIds.Count} tables in solution '{currentSolutionName}'");

			if (tableIds.Count == 0)
			{
				return CommandResult.Fail("No tables found in the solution. Please check the solution name and try again.");
			}


			output.WriteLine("Retrieving tables metadata...");
			query = new QueryExpression("entity");
			query.ColumnSet.AddColumns("logicalname");
			if (tableIds.Count == 1)
			{
				query.Criteria.AddCondition("entityid", ConditionOperator.Equal, tableIds[0]);
			}
			else
			{
				query.Criteria.AddCondition("entityid", ConditionOperator.In, tableIds.Cast<object>().ToArray());
			}
			query.NoLock = true;

			var tableNames = (await crm.RetrieveMultipleAsync(query))
				.Entities
				.Select(x => x.GetAttributeValue<string>("logicalname"))
				.Select(x => x.ToLowerInvariant())
				.ToArray();




			var (missingTables, graph) = await this.tableGraphBuilder.BuildGraphAsync(crm, tableNames, command.IncludeSecurityTables, command.SkipMissingTables);



			if (!command.SkipMissingTables && missingTables.HasMissingTables)
			{
				return CommandResult.Fail(missingTables.ToString());
			}


			var diagram = graph.ToMermaidDiagram();

			output.WriteLine(diagram, ConsoleColor.White);

			return CommandResult.Success();
		}
	}
}
