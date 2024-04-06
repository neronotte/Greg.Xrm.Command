namespace Greg.Xrm.Command.Commands.Table.Migration
{
	public class MigrationActionFullTable : IMigrationAction
	{
		public MigrationActionFullTable(string? tableName)
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				throw new ArgumentNullException(nameof(tableName), $"'{nameof(tableName)}' cannot be null or empty.");
			}

			TableName = tableName.ToLowerInvariant();
		}

		public string TableName { get; }

		public override string ToString()
		{
			return $"Full import on table <{TableName}>";
		}
	}
}
