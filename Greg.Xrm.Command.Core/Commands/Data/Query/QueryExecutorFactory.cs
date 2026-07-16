using System.Text.RegularExpressions;

namespace Greg.Xrm.Command.Commands.Data.Query
{

	public class QueryExecutorFactory : IQueryExecutorFactory
	{
		/// <summary>
		/// Stricter check using regex: matches $option= as it would appear
		/// in a real query string (avoids false positives from stray text).
		/// </summary>
		public static bool ContainsODataQueryStrict(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return false;

			// Matches e.g. ?$filter=..., &$select=..., $top=10
			var pattern = @"[\?&]?\$(filter|select|expand|orderby|top|skip|count|search|format|apply|compute|skiptoken|index)\s*=";
			return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
		}

		public IQueryExecutor DetectExecutorFromQueryText(string queryText)
		{
ArgumentNullException.ThrowIfNull(queryText);
			queryText = queryText.TrimStart();

			if (queryText.StartsWith('<'))
			{
				return new QueryExecutorFetchXml(queryText);
			}

			if (queryText.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
			{
				return new QueryExecutorSql(queryText);
			}

			if (ContainsODataQueryStrict(queryText))
			{
				return new QueryExecutorOData(queryText);
			}

			throw new NotSupportedException("Unsupported query format.");
		}
	}
}
