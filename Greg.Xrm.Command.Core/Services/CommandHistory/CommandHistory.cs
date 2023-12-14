namespace Greg.Xrm.Command.Services.CommandHistory
{
	class CommandHistory
	{
		public int MaxSize { get; set; } = 1000;

		public List<string> Commands { get; set; } = new List<string>();

		public void Add(params string[] command)
		{
			var commandText = string.Join(" ", command.Select(x => EncloseInQuotesIfContainsSpace(x)));

			this.Commands.Add(commandText);
			if (this.Commands.Count > this.MaxSize)
			{
				this.Commands.RemoveAt(0);
			}
		}

		private static string EncloseInQuotesIfContainsSpace(string command)
		{
			if (command.Contains(' '))
			{
				return $"\"{command}\"";
			}
			return command;
		}
	}

}
