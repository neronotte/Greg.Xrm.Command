namespace Greg.Xrm.Command.Services.Graphs
{
    public interface IDirectedArc<T>
		where T : INodeContent

	{
		/// <summary>
		/// Gets the starting node of the arc
		/// </summary>
		IDirectedNode<T> From { get; }

		/// <summary>
		/// Gets the final node of the arc
		/// </summary>
		IDirectedNode<T> To { get; }

		/// <summary>
		/// Gets the additional information associated with the arc, with the given key
		/// </summary>
		/// <typeparam name="T1">The type of the info</typeparam>
		/// <param name="key">The key of the info to return</param>
		/// <returns>The additional information associated with the arc, with the given key</returns>
		T1? GetAdditionalInfo<T1>(string key);
	}
}
