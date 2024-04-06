namespace Greg.Xrm.Command.Commands.Table.Migration
{
	public class MigrationActionTableWithoutColumn : IMigrationAction
	{
		public MigrationActionTableWithoutColumn(string? tableName, string columnName)
		{
			if (string.IsNullOrEmpty(tableName))
			{
				throw new ArgumentException($"'{nameof(tableName)}' cannot be null or empty.", nameof(tableName));
			}

			if (string.IsNullOrEmpty(columnName))
			{
				throw new ArgumentException($"'{nameof(columnName)}' cannot be null or empty.", nameof(columnName));
			}

			TableName = tableName;
			ColumnName = columnName;
		}

		public string TableName { get; }

		public string ColumnName { get; }

		public string[] GetRelatedTableNames()
		{
			return ColumnName
				.Split(",")
				.Select(x => x.Trim())
				.Select(x =>
				{
					var startIndex = x.IndexOf("(") + 1;
					var len = x.IndexOf(")") - startIndex;
					return x.Substring(startIndex, len);
				})
				.Distinct()
				.ToArray();
		}


		public override string ToString()
		{
			return $"Import table <{TableName}> without column(s) <{ColumnName}>";
		}
	}
}
