namespace Greg.Xrm.Command.Services.Graphs
{
	public class ConsistencyException : Exception
	{
		public ConsistencyException() { }
		public ConsistencyException(string message) : base(message) { }
		public ConsistencyException(string message, Exception inner) : base(message, inner) { }
	}
}
