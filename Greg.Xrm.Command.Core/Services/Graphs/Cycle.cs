using System.Collections;

namespace Greg.Xrm.Command.Services.Graphs
{
	public class Cycle<T> : IReadOnlyList<IDirectedArc<T>>
		where T : INodeContent
	{
		private readonly List<IDirectedArc<T>> arcs = new();


		public Cycle(IEnumerable<IDirectedArc<T>> nodes)
		{
			arcs.AddRange(nodes);
		}


		#region IReadOnlyList<DirectedNode<T>>

		public IDirectedArc<T> this[int index] => arcs[index];

		public int Count => arcs.Count;

		public IEnumerator<IDirectedArc<T>> GetEnumerator()
		{
			return arcs.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IEquality

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				return arcs.Aggregate(0, (current, item) => current ^ (item != null ? item.GetHashCode() : 0)) ^ arcs.Count;
			}
		}

		public override bool Equals(object? obj)
		{
			if (obj == null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj is not Cycle<T> other) return false;

			return arcs.SequenceEqual(other.arcs);
		}

		#endregion



		/// <summary>
		/// Indicates whether the Cycle is a self-loop on a given node.
		/// </summary>
		public bool IsAutoCycle
		{
			get => arcs.Count == 1 && arcs[0].From.Equals(arcs[0].To);
		}


		/// <summary>
		/// Indicates whether the Cycle presents only references to nodes that are part of the cycle.
		/// </summary>
		public bool IsSelfContained
		{
			get
			{
				var nodesInCicle = arcs.Select(x => x.From).ToHashSet();

				foreach (var node in nodesInCicle)
				{
					foreach (var referencedNode in node.OutboundArcs.Select(x => x.To))
					{
						if (!nodesInCicle.Contains(referencedNode))
							return false;
					}
				}

				return true;
			}
		}




		/// <summary>
		/// Indicates whether any of the tables in the cycle contains a self-loop.
		/// </summary>
		public bool ContainsAutoCycle
		{
			get
			{
				var nodesInCicle = arcs.Select(x => x.From).ToHashSet();

				foreach (var node in nodesInCicle)
				{
					if (node.HasAutoCycle) return true;
				}

				return false;
			}
		}
	}
}
