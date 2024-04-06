using System.Text;

namespace Greg.Xrm.Command.Commands.Table.Migration
{
	public class MissingTableCache
	{
		private readonly Dictionary<string, List<string>> cache = new();

		public void Add(string tableName, string referencedByName)
		{
			if (!cache.ContainsKey(tableName))
			{
				cache[tableName] = new List<string>();
			}

			if (cache[tableName].Contains(referencedByName))
			{
				return;
			}

			cache[tableName].Add(referencedByName);
		}


		public bool HasMissingTables => cache.Count > 0;

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine("The following tables are missing from the list:");
			foreach (var item in cache)
			{
				sb.AppendLine($"  - {item.Key} is referenced by {string.Join(", ", item.Value.Order())}");
			}

			return sb.ToString();
		}
	}
}
