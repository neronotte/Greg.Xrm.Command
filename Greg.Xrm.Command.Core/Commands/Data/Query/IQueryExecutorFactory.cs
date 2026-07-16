namespace Greg.Xrm.Command.Commands.Data.Query
{
	public interface IQueryExecutorFactory
	{
		IQueryExecutor DetectExecutorFromQueryText(string queryText);
	}
}
