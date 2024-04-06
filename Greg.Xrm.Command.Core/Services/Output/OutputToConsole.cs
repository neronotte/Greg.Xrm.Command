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


		public IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker = null)
		{
			colorPicker ??= (i, _) => Console.ForegroundColor;
			var headers = rowHeaders();
			var rows = collection.Select(rowData).ToList();

			var columnWidths = new int[headers.Length];
			int i = 0;
			for (i = 0; i < headers.Length; i++)
			{
				columnWidths[i] = Math.Max(headers[i].Length, rows.Count == 0 ? 0 : rows.Max(_ => _[i].Length));
			}

			var header = "| " + string.Join(" | ", headers.Select((_, i) => _.PadRight(columnWidths[i]))) + " |";
			this.WriteLine(header);

			var separator = "|-" + string.Join("-|-", columnWidths.Select(_ => new string('-', _))) + "-|";
			this.WriteLine(separator);

			i = 0;
			foreach (var row in rows)
			{
				this.Write("| ");

				for (var j = 0; j < row.Length; j++)
				{
					var color = colorPicker(j, collection[i]) ?? Console.ForegroundColor;
					this.Write(row[j].PadRight(columnWidths[j]), color);
					this.Write(" | ");
				}
				this.WriteLine();
				i++;
			}
			this.WriteLine();

			return this;
		}
	}
}
