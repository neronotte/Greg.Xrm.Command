using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Newtonsoft.Json;
using Spectre.Console;

namespace Greg.Xrm.Command.Commands.Security.BusinessUnits
{
	public class ListCommandExecutor(
		IOutput output,
		IOrganizationServiceRepository connections,
		IBusinessUnitRepository businessUnits,
		IAnsiConsole console) : ICommandExecutor<ListCommand>
	{
		public async Task<CommandResult> ExecuteAsync(ListCommand command, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				var json = command.Format == BusinessUnitOutputFormat.Json;
				if (!json) output.Write("Connecting to the current dataverse environment...");
				var crm = await connections.GetCurrentConnectionAsync();
				if (!json) output.WriteLine("Done", ConsoleColor.Green);
				if (!json) output.Write("Retrieving business units...");
				var units = await businessUnits.GetAllAsync(crm, cancellationToken);
				var roots = BuildHierarchy(units, cancellationToken);
				if (json)
				{
					output.WriteLine(JsonConvert.SerializeObject(new { BusinessUnits = roots }, Formatting.Indented));
					return CommandResult.Success();
				}
				output.WriteLine("Done", ConsoleColor.Green);
				var tree = new Tree($"Business units ({units.Count})");
				foreach (var root in roots)
					AddChildren(tree.AddNode(Label(root)), root.Children);
				if (roots.Count == 0) tree.AddNode("No business units");
				console.Write(tree);
				var result = CommandResult.Success();
				result["Count"] = units.Count;
				result["RootCount"] = roots.Count;
				return result;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
			catch (Exception exception) { return CommandResult.Fail(exception.Message, exception); }
		}

		private sealed record BusinessUnitNode(Guid Id, string Name, Guid? ParentId)
		{
			public List<BusinessUnitNode> Children { get; } = [];
		}

		private static IReadOnlyList<BusinessUnitNode> BuildHierarchy(IReadOnlyList<BusinessUnit> units, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var nodes = units.OrderBy(unit => unit.name, StringComparer.OrdinalIgnoreCase).ThenBy(unit => unit.Id)
				.Select(unit => new BusinessUnitNode(unit.Id, unit.name ?? string.Empty, unit.parentbusinessunitid?.Id)).ToArray();
			var byId = nodes.ToDictionary(node => node.Id);
			var roots = new List<BusinessUnitNode>();
			foreach (var node in nodes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (node.ParentId.HasValue && byId.TryGetValue(node.ParentId.Value, out var parent))
					parent.Children.Add(node);
				else
					roots.Add(node);
			}
			var pending = new Queue<BusinessUnitNode>(roots);
			var reachable = 0;
			while (pending.TryDequeue(out var node))
			{
				cancellationToken.ThrowIfCancellationRequested();
				reachable++;
				foreach (var child in node.Children) pending.Enqueue(child);
			}
			if (reachable != nodes.Length)
				throw new InvalidOperationException("The business unit hierarchy contains a cycle.");
			return roots;
		}

		private static string Label(BusinessUnitNode node) => Markup.Escape($"{node.Name} ({node.Id})");

		private static void AddChildren(TreeNode parent, IReadOnlyList<BusinessUnitNode> children)
		{
			foreach (var child in children)
				AddChildren(parent.AddNode(Label(child)), child.Children);
		}
	}
}