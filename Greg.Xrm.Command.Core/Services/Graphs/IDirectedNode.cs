namespace Greg.Xrm.Command.Services.Graphs
{
    public interface IDirectedNode<T>
		where T : INodeContent
	{
		/// <summary>
		/// Gets the content of the node
		/// </summary>
		T Content { get; }

		/// <summary>
		/// Gets the arcs that end into this node
		/// </summary>
		IReadOnlyList<IDirectedArc<T>> InboundArcs { get; }

		/// <summary>
		/// Gets the arcs that start from this node
		/// </summary>
		IReadOnlyList<IDirectedArc<T>> OutboundArcs { get; }


		/// <summary>
		/// Indicates whether the node has a self-loop
		/// </summary>
		bool HasAutoCycle { get; }


		/// <summary>
		/// Returns the autocycle, if any
		/// </summary>
		IDirectedArc<T>? AutoCycle { get; }
	}
}
