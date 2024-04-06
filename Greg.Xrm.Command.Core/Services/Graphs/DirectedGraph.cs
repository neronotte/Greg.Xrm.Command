using System.Collections;
using System.Text;

namespace Greg.Xrm.Command.Services.Graphs
{
    /// <summary>
    /// Implements the logic of a directed graph
    /// </summary>
    /// <typeparam name="T">The type of the nodes</typeparam>
    public class DirectedGraph<T> : ICloneable, IEnumerable<T>
		where T : INodeContent
	{
		private readonly object syncRoot = new();
		private readonly Dictionary<object, DirectedNode<T>> nodes;
		private readonly List<DirectedArc<T>> arcs = new();

		public DirectedGraph()
		{
			nodes = new Dictionary<object, DirectedNode<T>>();
		}


		public DirectedGraph(IEnumerable<T> nodes)
		{
			this.nodes = nodes.ToDictionary(x => x.Key, x => new DirectedNode<T>(x));
		}




		/// <summary>
		/// Gets the node with the given key
		/// </summary>
		/// <param name="key">The key that identifies the node to return.</param>
		/// <returns>The node having the specified key</returns>
		/// <exception cref="ArgumentException">If no node with the given key existes in the graph</exception>
		public T this[object key]
		{
			get
			{
				if (!nodes.TryGetValue(key, out var node))
					throw new ArgumentException($"Node with key {key} does not exists", nameof(key));

				return node.Content;
			}
		}

		/// <summary>
		/// Tries to get the node with the given key. If not found, returns null.
		/// </summary>
		/// <param name="key">The key that identifies the node to return.</param>
		/// <returns>The node with the given key, or null.</returns>
		public T? TryGet(object key)
		{
			if (nodes.TryGetValue(key, out var node))
				return node.Content;
			return default;
		}


		public IDirectedNode<T>? TryGetNodeFor(object key)
		{
			if (nodes.TryGetValue(key, out var node))
				return node;

			return default;
		}



		/// <summary>
		/// Indicates whether the graph has nodes or not
		/// </summary>
		public bool HasNodes => nodes.Count > 0;

		/// <summary>
		/// Gets the total number of nodes in the graph
		/// </summary>
		public int NodeCount => nodes.Count;

		/// <summary>
		/// Gets the total number of arcs in the graph
		/// </summary>
		public int ArcCount => arcs.Count;


		/// <summary>
		/// Adds a node to the current graph
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public DirectedGraph<T> AddNode(T node)
		{
			lock (syncRoot)
			{
				if (nodes.ContainsKey(node.Key))
				{
					throw new ArgumentException($"Node with key {node.Key} already exists", nameof(node));
				}
				nodes[node.Key] = new DirectedNode<T>(node);
			}
			return this;
		}


		/// <summary>
		/// Adds an arc to the current directed graph
		/// </summary>
		/// <param name="from">The starting node of the arc</param>
		/// <param name="to">The final node of the arc</param>
		/// <param name="additionalInfo">Additional metadata to be provided on the arc</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If from or to does not match any node in the current graph</exception>
		/// <exception cref="InvalidOperationException">If the arc already exists</exception>
		/// <exception cref="ConsistencyException">If there is a consistency error on the graph (e.g. an arc exists but it's tied only to one of the two ends).</exception>
		public DirectedGraph<T> AddArch(T from, T to, IReadOnlyDictionary<string, object>? additionalInfo = null)
		{
			lock (syncRoot)
			{
				if (!nodes.TryGetValue(from.Key, out var fromNode))
					throw new ArgumentException($"Node with key {from.Key} does not exists", nameof(from));
				if (!nodes.TryGetValue(to.Key, out var toNode))
					throw new ArgumentException($"Node with key {to.Key} does not exists", nameof(to));


				var arc = new DirectedArc<T>(fromNode, toNode, additionalInfo);

				if (fromNode.OutboundArcs.Contains(arc))
					throw new InvalidOperationException($"Arc from {from.Key} to {to.Key} already exists");
				if (toNode.InboundArcs.Contains(arc))
					throw new ConsistencyException($"An arc from {from.Key} to {to.Key} exists as outbound arc, but not as inbound arc. It should not be possible!");
				if (arcs.Contains(arc))
					throw new ConsistencyException($"An arc from {from.Key} to {to.Key} exists as outbound and inbound arc, but not in the graph arc collection. It should not be possible!");

				fromNode.OutboundArcs.Add(arc);
				toNode.InboundArcs.Add(arc);
				arcs.Add(arc);
			}

			return this;
		}



		#region ICloneable

		/// <summary>
		/// Clones the current graph
		/// </summary>
		/// <returns>A graph that is a clone of the current one</returns>
		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Clones the current graph
		/// </summary>
		/// <returns>A graph that is a clone of the current one</returns>
		public DirectedGraph<T> Clone()
		{
			var clone = new DirectedGraph<T>(nodes.Values.Select(x => x.Content));
			foreach (var arc in arcs)
			{
				clone.AddArch(arc.From.Content, arc.To.Content, arc);
			}
			return clone;
		}

		#endregion


		/// <summary>
		/// Returns the list of nodes that have no outbound arcs
		/// </summary>
		/// <returns>The list of nodes that have no outbound arcs</returns>
		public IReadOnlyList<T> GetLeaves()
		{
			return nodes.Values.Where(x => x.OutboundArcs.Count == 0).Select(x => x.Content).ToList();
		}


		/// <summary>
		/// Returns all the cycles that can be found in the graph.
		/// </summary>
		/// <returns>All the cycles that can be found in the graph.</returns>
		public List<Cycle<T>> FindAllCycles()
		{
			var visitedNodes = new HashSet<object>();
			var allCycles = new List<Cycle<T>>();

			foreach (var node in nodes.Values)
			{
				var path = new List<DirectedArc<T>>();
				FindAllCycles(node, visitedNodes, path, allCycles);
			}

			return allCycles.Distinct().ToList();
		}





		private void FindAllCycles(DirectedNode<T> node, HashSet<object> visitedNodes, List<DirectedArc<T>> path, List<Cycle<T>> allCycles)
		{
			visitedNodes.Add(node.Content.Key);

			foreach (var outboundArc in node.OutboundArcs)
			{
				var nextNode = outboundArc.To;
				path.Add(outboundArc);

				if (!nodes.ContainsKey(nextNode.Content.Key))
				{
					continue;
				}


				DirectedArc<T>? pathElement;
				if (!visitedNodes.Contains(nextNode.Content.Key))
				{
					FindAllCycles(nextNode, visitedNodes, path, allCycles);
				}
				else if ((pathElement = path.Find(x => x.From == nextNode)) != null)
				{
					// Found a cycle
					var cycle = new List<DirectedArc<T>>();
					for (int i = path.IndexOf(pathElement);
						i < path.Count;
						i++)
					{
						cycle.Add(path[i]);
					}
					allCycles.Add(new Cycle<T>(cycle));
				}

				path.RemoveAt(path.Count - 1);
			}
		}

		/// <summary>
		/// Creates and returns a new graph that is a clone of the current graph, but without the specified nodes.
		/// Removes all the arcs that are connected to the given node.
		/// </summary>
		/// <param name="nodeContents">The nodes to remove from the graph</param>
		/// <returns></returns>
		public DirectedGraph<T> RemoveNodes(params T[] nodeContents)
		{
			return RemoveNodes(nodeContents.AsEnumerable());
		}

		/// <summary>
		/// Creates and returns a new graph that is a clone of the current graph, but without the specified nodes.
		/// Removes all the arcs that are connected to the given node.
		/// </summary>
		/// <param name="nodeContents">The nodes to remove from the graph</param>
		/// <returns></returns>
		public DirectedGraph<T> RemoveNodes(IEnumerable<T> nodeContents)
		{
			foreach (var nodeContent in nodeContents)
			{
				var node = nodes[nodeContent.Key];
				foreach (var arc in node.OutboundArcs)
				{
					arc.To.InboundArcs.Remove(arc);
					arcs.Remove(arc);
				}
				foreach (var arc in node.InboundArcs)
				{
					arc.From.OutboundArcs.Remove(arc);
					arcs.Remove(arc);
				}

				nodes.Remove(node.Content.Key);
			}

			return this;
		}


		public override string ToString()
		{
			return $"Nodes: {NodeCount}, Arcs: {ArcCount}";
		}


		#region IEnumerable<T>

		public IEnumerator<T> GetEnumerator()
		{
			return nodes.Values.Select(x => x.Content).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion



		public virtual string ToMermaidDiagram()
		{
			var sb = new StringBuilder();

			sb.AppendLine("classDiagram");
			foreach (var node in nodes)
			{
				sb.Append("    class ").AppendLine(node.Value.Content.Key.ToString());
			}
			foreach (var arc in arcs)
			{
				sb.AppendLine($"    {arc.From.Content.Key.ToString()} --> {arc.To.Content.Key.ToString()}");
			}

			return sb.ToString();
		}
	}
}
