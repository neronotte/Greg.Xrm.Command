namespace Greg.Xrm.Command.Services.Output
{
	public class OutputToConsole : IOutput
	{
		private readonly object syncRoot = new();

		public IOutput Write(object? text)
		{
			lock(syncRoot)
			{
				Console.Write(text);
			}
			return this;
		}

		public IOutput Write(object? text, ConsoleColor color)
		{
			lock(syncRoot)
			{
				var currentColor = Console.ForegroundColor;
				Console.ForegroundColor = color;
				Console.Write(text);
				Console.ForegroundColor = currentColor;
			}
			return this;
		}

		public IOutput WriteLine(object? text)
		{
			lock (syncRoot)
			{
				Console.WriteLine(text);
			}
			return this;
		}

		public IOutput WriteLine()
		{
			lock (syncRoot)
			{
				Console.WriteLine();
			}
			return this;
		}

		public IOutput WriteLine(object? text, ConsoleColor color)
		{
			lock (syncRoot)
			{
				var currentColor = Console.ForegroundColor;
				Console.ForegroundColor = color;
				Console.WriteLine(text);
				Console.ForegroundColor = currentColor;
			}
			return this;
		}
	}
}
