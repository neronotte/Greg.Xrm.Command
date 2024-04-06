using Greg.Xrm.Command.Services.Graphs;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Table.Migration;

    public class TableGraphBuilder
{
	private readonly IOutput output;

	public TableGraphBuilder(IOutput output)
	{
		this.output = output;
	}


	public async Task<(MissingTableCache, DirectedGraph<TableModel>)> BuildGraphAsync(IOrganizationServiceAsync2 crm, string[] tableNames, bool includeSecurityTables, bool skipMissingTables)
	{
		if (!includeSecurityTables)
		{
			tableNames = tableNames.Except(SecurityTables.SecurityTableNames).ToArray();
		}


		// fill the graph with nodes (the tables of our datamodel)
		var graph = new DirectedGraph<TableModel>();
		foreach (var table in tableNames)
		{
			graph.AddNode(new TableModel(table));
		}



		var missingTables = new MissingTableCache();

		foreach (var tableName in tableNames)
		{
			var metadata = ((RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
			{
				LogicalName = tableName,
				EntityFilters = EntityFilters.Attributes
			})).EntityMetadata;


			var from = graph[tableName];

			var temp = metadata.Attributes.OfType<LookupAttributeMetadata>()
				.SelectMany(x => x.Targets.Select(y => new { Field = x.LogicalName, Table = y.ToLowerInvariant() }))
				.Where(x => includeSecurityTables || !SecurityTables.SecurityTableNames.Contains(x.Table))
				.ToList();

			var temp2 = temp.GroupBy(x => x.Table)
				.Select(x => new
				{
					Table = x.Key,
					Columns = x.Select(y => y.Field).ToArray()
				})
				.ToList();

			foreach (var item in temp2)
			{
				var to = graph.TryGet(item.Table);
				if (to == null)
				{
					if (skipMissingTables)
					{
						output.WriteLine($"Table '{item.Table}' not found in the solution. Skipping the relationship with table '{tableName}'.", ConsoleColor.Yellow);
						continue;
					}

					missingTables.Add(item.Table, tableName);
					continue;
				}

				graph.AddArch(from, to, new Dictionary<string, object> { { "columns", item.Columns } });
			}
		}

		output.WriteLine("Done", ConsoleColor.Green);

		return (missingTables, graph);
	}
}
