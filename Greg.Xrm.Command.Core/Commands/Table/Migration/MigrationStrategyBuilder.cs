using Greg.Xrm.Command.Services.Graphs;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Migration
{
    public static class MigrationStrategyBuilder
	{
		public static MigrationStrategyResult Build(DirectedGraph<TableModel> graph)
		{
			// find the leaf tables
			// if there is no leaf table, but we have then we have a circular dependency

			var result = new MigrationStrategyResult();
			var currentGraph = graph;

			var iterationCount = 0;
			while (currentGraph.HasNodes && !result.HasError)
			{
				result.MigrationActions.Add(new MigrationActionLog($"*** Iteration {++iterationCount} ***"));

				var leafList = currentGraph.GetLeaves();
				if (leafList.Count > 0)
				{
					foreach (var leaf in leafList.OrderBy(x => x.Key))
					{
						result.Add(leaf.ToString());
					}

					currentGraph = currentGraph.Clone().RemoveNodes(leafList);
				}
				else
				{
					// handle circular dependency
					var succeeded = TryManageCycles(currentGraph, result, out var newGraph);
					if (!succeeded)
					{
						return result;
					}

					currentGraph = newGraph;
				}
			}

			return result;
		}





		private static bool TryManageCycles(DirectedGraph<TableModel> graph, MigrationStrategyResult result, out DirectedGraph<TableModel> newGraph)
		{
			newGraph = graph;

			var cycles = newGraph.FindAllCycles();
			if (cycles.Count == 0)
			{
				result.SetError($"The list contains {graph.NodeCount} tables, but no cycles have been found, and no leafs are present. Please check with your admin.");
				return false;
			}

			if (cycles.Count == 1)
			{
				return TryBreakCycle(newGraph, cycles[0], result, out newGraph);
			}

			var autoCyclesSelfContained = cycles.Where(x => x.IsAutoCycle && x.IsSelfContained).ToList();
			if (autoCyclesSelfContained.Count > 0)
			{
				foreach (var cycle in autoCyclesSelfContained)
				{
					if (!TryBreakCycle(newGraph, cycle, result, out newGraph))
						return false;
				}

				return true;
			}

			var selfContainedCycles = cycles.Where(x => x.IsSelfContained).ToList();
			if (selfContainedCycles.Count > 0)
			{
				foreach (var cycle in selfContainedCycles)
				{
					if (!TryBreakCycle(newGraph, cycle, result, out newGraph))
						return false;
				}

				return true;
			}



			var sb = new StringBuilder();
			sb.AppendLine($"The list contains still {graph.NodeCount} tables, but {cycles.Count} cycle{(cycles.Count > 1 ? "s have" : "has")} been found that we don't know how to manage: ");
			foreach (var cycle in cycles)
			{
				sb.AppendLine($"  - {string.Join(", ", cycle.Select(x => x.ToString()))}");
			}
			sb.AppendLine("You need to identify a manual solution to work with those cycles.");

			result.SetError(sb.ToString());
			return false;
		}




		private static bool TryBreakCycle(DirectedGraph<TableModel> graph, Cycle<TableModel> loop, MigrationStrategyResult result, out DirectedGraph<TableModel> newGraph)
		{
			newGraph = graph;

			if (loop.IsAutoCycle)
			{
				var arc = loop[0];
				var itemColumns = arc.GetAdditionalInfo<string[]>("columns")?
					.Select(x => $"{x} ({arc.To})")
					.ToArray() ?? Array.Empty<string>();

				result.Add(new MigrationActionTableWithoutColumn(arc.From.ToString(), string.Join(", ", itemColumns)));
				result.Add(new MigrationActionUpdateTableColumn(arc.From.ToString(), string.Join(", ", itemColumns)));

				newGraph = newGraph.Clone().RemoveNodes(arc.From.Content);

				return true;
			}



			IMigrationAction? lastMigrationAction = null;
			for (int i = 0; i < loop.Count; i++)
			{
				var arc = loop[i];

				var tableToImport = arc.From;
				var columns = arc.GetAdditionalInfo<string[]>("columns")?
					.Select(x => $"{x} ({arc.To})")
					.ToArray() ?? Array.Empty<string>();


				if (i == 0)
				{
					if (tableToImport.HasAutoCycle)
					{
						var columns1 = (tableToImport.AutoCycle?.GetAdditionalInfo<string[]>("columns") ?? Array.Empty<string>())
							.Select(x => $"{x} ({tableToImport})")
							.ToArray();

						var columns2 = columns1
							.Union(columns)
							.ToArray();

						result.Add(new MigrationActionTableWithoutColumn(arc.From.ToString(), string.Join(", ", columns2)));
						result.Add(new MigrationActionUpdateTableColumn(arc.From.ToString(), string.Join(", ", columns1)));
						lastMigrationAction = new MigrationActionUpdateTableColumn(arc.From.ToString(), string.Join(", ", columns));
					}
					else
					{
						result.Add(new MigrationActionTableWithoutColumn(arc.From.ToString(), string.Join(", ", columns)));
						lastMigrationAction = new MigrationActionUpdateTableColumn(arc.From.ToString(), string.Join(", ", columns));
					}
				}
				else
				{
					if (tableToImport.HasAutoCycle)
					{
						var columns1 = (tableToImport.AutoCycle?.GetAdditionalInfo<string[]>("columns") ?? Array.Empty<string>())
							.Select(x => $"{x} ({tableToImport})")
							.ToArray();

						result.Add(new MigrationActionTableWithoutColumn(arc.From.ToString(), string.Join(", ", columns1)));
						result.Add(new MigrationActionUpdateTableColumn(arc.From.ToString(), string.Join(", ", columns1)));
					}
					else
					{
						result.Add(arc.From.ToString());
					}
				}

				newGraph = newGraph.Clone().RemoveNodes(arc.From.Content);
			}

			if (lastMigrationAction != null)
				result.Add(lastMigrationAction);

			return true;
		}
	}
}
