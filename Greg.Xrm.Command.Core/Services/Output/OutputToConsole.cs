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


		public IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, string, ConsoleColor?>? colorPicker = null)
		{
			colorPicker ??= (i, _) => Console.ForegroundColor;
			var headers = rowHeaders();
			var rows = collection.Select(rowData).ToList();

			var columnWidths = new int[headers.Length];
			for (var i = 0; i < headers.Length; i++)
			{
				columnWidths[i] = Math.Max(headers[i].Length, rows.Max(_ => _[i].Length));
			}

			var header = "| " + string.Join(" | ", headers.Select((_, i) => _.PadRight(columnWidths[i]))) + " |";
			Console.WriteLine(header);

			var separator = "|-" + string.Join("-|-", columnWidths.Select(_ => new string('-', _))) + "-|";
			Console.WriteLine(separator);

			foreach (var row in rows)
			{
				Console.Write("| ");

				for (var i = 0; i < row.Length; i++)
				{
					var color = colorPicker(i, row[i]) ?? Console.ForegroundColor;
					Console.Write(row[i].PadRight(columnWidths[i]), color);
					Console.Write(" | ");
				}
				Console.WriteLine();
			}
			Console.WriteLine();

			return this;
		}
	}
}
