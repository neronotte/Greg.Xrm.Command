namespace Greg.Xrm.Command.Commands.Table.Migration
{
	public class MigrationStrategyResult
	{
		private MigrationStrategyResult(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}


		public MigrationStrategyResult()
		{
			ErrorMessage = string.Empty;
		}

		public static MigrationStrategyResult Error(string errorMessage) => new MigrationStrategyResult(errorMessage);


		public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
		public string ErrorMessage { get; private set; }

		public List<IMigrationAction> MigrationActions { get; } = new List<IMigrationAction>();

		public MigrationStrategyResult Add(string? tableName)
		{
			return Add(new MigrationActionFullTable(tableName));
		}

		public MigrationStrategyResult Add(IMigrationAction migrationAction)
		{
			MigrationActions.Add(migrationAction);
			return this;
		}

		internal void SetError(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}
	}
}
