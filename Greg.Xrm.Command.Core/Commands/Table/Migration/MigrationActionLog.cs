namespace Greg.Xrm.Command.Commands.Table.Migration
{
	internal class MigrationActionLog : IMigrationAction
	{
		private readonly string message;

		public MigrationActionLog(string message)
		{
			this.message = message;
		}

		public string TableName => string.Empty;

		public override string ToString()
		{
			return message;
		}
	}
}
