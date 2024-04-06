using Greg.Xrm.Command.Commands.Table.Migration;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Commands.Table
{
    public class TableDefineMigrationStrategyCommandExecutor : ICommandExecutor<TableDefineMigrationStrategyCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly TableGraphBuilder tableGraphBuilder;

		public TableDefineMigrationStrategyCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			TableGraphBuilder tableGraphBuilder)
		{
			this.output = output;
			this.organizationServiceRepository = organizationServiceRepository;
			this.tableGraphBuilder = tableGraphBuilder;
		}


		public async Task<CommandResult> ExecuteAsync(TableDefineMigrationStrategyCommand command, CancellationToken cancellationToken)
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



			output.WriteLine("Retrieving tables metadata...");
			query = new QueryExpression("entity");
			query.ColumnSet.AddColumns("logicalname");
			query.Criteria.AddCondition("entityid", ConditionOperator.In, tableIds.Cast<object>().ToArray());
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


			output.WriteLine($"Found {graph.NodeCount} table metadata in solution '{currentSolutionName}':");
			foreach (var table in graph)
			{
				output.WriteLine($"  - {table}");
			}
			output.WriteLine();







			output.WriteLine("Building the data tree...");

			var result = MigrationStrategyBuilder.Build(graph);

			if (result.MigrationActions.Count > 0)
			{
				output.WriteLine($"The migration strategy is:");

				var padding = result.MigrationActions.Count.ToString().Length;

				var i = 0;
				foreach (var migrationAction in result.MigrationActions)
				{
					if (migrationAction is MigrationActionLog)
					{
						if (command.Verbose)
						{
							output.WriteLine(migrationAction, ConsoleColor.Cyan);
						}

						continue;
					}


					var additionalInfo = string.Empty;
					if (command.Verbose)
					{
						var tableName = migrationAction.TableName;
						var table = graph.TryGetNodeFor(tableName);

						var dependsOn = table?.OutboundArcs.Select(x => x.To.Content.Key.ToString()).Distinct().ToArray() ?? Array.Empty<string>();
						if (migrationAction is MigrationActionTableWithoutColumn ma)
						{
							dependsOn = dependsOn.Except(ma.GetRelatedTableNames()).ToArray();
						}
						if (migrationAction is MigrationActionUpdateTableColumn ma2)
						{
							dependsOn = ma2.GetRelatedTableNames();
						}
						if (dependsOn.Length > 0)
						{
							additionalInfo = $" (depends on: {string.Join(", ", dependsOn.Order())})";
						}
					}


					i++;
					output.Write($"  STEP {i.ToString().PadLeft(padding)}) ", ConsoleColor.DarkGray);
					output.Write(migrationAction);
					output.WriteLine(additionalInfo, ConsoleColor.DarkGray);
				}
			}

			if (result.HasError)
			{
				return CommandResult.Fail(result.ErrorMessage);
			}



			return CommandResult.Success();
		}
	}
}
