using Spectre.Console;

namespace Greg.Xrm.Command.Services.Output
{
	public class OutputToAnsiConsole(IAnsiConsole ansiConsole) : IOutput
	{
		private readonly object syncRoot = new();

		public IOutput Write(object? text)
		{
			lock (syncRoot)
			{
				ansiConsole.Write(new Text(text?.ToString() ?? ""));
			}
			return this;
		}

		public IOutput Write(object? text, ConsoleColor color)
		{
			lock (syncRoot)
			{
				ansiConsole.Write(new Text(text?.ToString() ?? "", DrawingColor(color)));
			}
			return this;
		}

		public IOutput WriteLine(object? text)
		{
			lock (syncRoot)
			{
				ansiConsole.Write(new Text(text?.ToString() ?? ""));
				ansiConsole.WriteLine();
			}
			return this;
		}

		public IOutput WriteLine()
		{
			lock (syncRoot)
			{
				ansiConsole.WriteLine();
			}
			return this;
		}

		public IOutput WriteLine(object? text, ConsoleColor color)
		{
			lock (syncRoot)
			{
				ansiConsole.Write(new Text(text?.ToString() ?? "", DrawingColor(color)));
				ansiConsole.WriteLine();
			}
			return this;
		}


		public IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker = null)
		{
			var table = new Table()
				.RoundedBorder()
				.BorderColor(Color.CornflowerBlue)
				.ShowRowSeparators();

			foreach (var col in rowHeaders())
			{
				//table.AddColumn(col, c => c.NoWrap().LeftAligned());
				table.AddColumn(col, c => c.NoWrap().LeftAligned());
			}

			foreach (var row in collection)
			{
				var columns = rowData(row);
				var renderers = new Text[columns.Length];
				for (int i = 0; i < columns.Length; i++)
				{
					var color = colorPicker?.Invoke(i, row);
					if (color.HasValue)
					{
						renderers[i] = new Text(columns[i], new Style(foreground: DrawingColor(color.Value)));
					}
					else
					{
						renderers[i] = new Text(columns[i]);
					}
				}
				table.AddRow(renderers);
			}

			ansiConsole.Write(table);
			ansiConsole.WriteLine();

			return this;
		}


		public static Color DrawingColor(ConsoleColor color)
		{
			return color switch
			{
				ConsoleColor.Black => Color.Black,
				ConsoleColor.Blue => Color.Blue,
				ConsoleColor.Cyan => Color.Cyan1,
				ConsoleColor.DarkBlue => Color.DarkBlue,
				ConsoleColor.DarkGray => Color.Gray50,
				ConsoleColor.DarkGreen => Color.DarkGreen,
				ConsoleColor.DarkMagenta => Color.DarkMagenta,
				ConsoleColor.DarkRed => Color.DarkRed,
				ConsoleColor.DarkYellow => Color.DarkGoldenrod,
				ConsoleColor.Gray => Color.Gray,
				ConsoleColor.Green => Color.Green1,
				ConsoleColor.Magenta => Color.Magenta,
				ConsoleColor.Red => Color.Red,
				ConsoleColor.White => Color.White,
				ConsoleColor.DarkCyan => Color.SkyBlue2,
				ConsoleColor.Yellow => Color.Yellow,
				_ => Color.Grey,
			};
		}
	}
}
