using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

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
			Text getRenderer(string? text, ConsoleColor? color)
			{
				text = text ?? string.Empty;

				if (color.HasValue)
				{
					return new Text(text, new Style(foreground: DrawingColor(color.Value)));
				}
				else
				{
					return new Text(text);
				}
			}



			var table = new Table()
				.RoundedBorder()
				.BorderColor(Color.SkyBlue2)
				.ShowRowSeparators();

			var calculator = new RowLengthCalculator();

			calculator.NewRow();
			foreach (var col in rowHeaders())
			{
				//table.AddColumn(col, c => c.NoWrap().LeftAligned());
				table.AddColumn(col, c => c.NoWrap().LeftAligned());
				calculator.AddColumn(col);
			}
			calculator.EndRow();

			foreach (var row in collection)
			{
				calculator.NewRow();
				var columns = rowData(row);

				var renderers = new Text[columns.Length];
				for (int i = 0; i < columns.Length; i++)
				{
					var columnValue = columns[i];
					var color = colorPicker?.Invoke(i, row);
					renderers[i] = getRenderer(columnValue, color);
					calculator.AddColumn(columnValue);
				}
				calculator.EndRow();

				table.AddRow(renderers);
			}

			//ansiConsole.WriteLine("AnsiConsole.Profile.Width: " + ansiConsole.Profile.Width);
			//ansiConsole.WriteLine("MaxRowLength:              " + calculator.MaxRowLength);

			// if the table fits within the console width, render it as a table
			// otherwise let's render it as a tree view, because tables won't show up properly in the console if they are too wide
			if (calculator.MaxRowLength < ansiConsole.Profile.Width)
			{
				ansiConsole.Write(table);
				ansiConsole.WriteLine();
				return this;
			}





			Markup getRendererForTree(string columnName, int maxColumnNameLength, string? text, Color? columnNameColor = null, ConsoleColor? color = null)
			{
				text ??= string.Empty;
				columnNameColor ??= Color.SkyBlue2;
				var actualColor = DrawingColor(color ?? Console.ForegroundColor);

				var sb = new StringBuilder();
				sb.Append('[');
				sb.Append(columnNameColor.ToString());
				sb.Append(']');
				sb.Append(Markup.Escape((columnName+":").PadRight(maxColumnNameLength)));
				sb.Append("[/]");
				sb.Append('[');
				sb.Append(actualColor.ToString());
				sb.Append(']');
				sb.Append(Markup.Escape(text));
				sb.Append("[/]");

				return new Markup(sb.ToString());
			}



			var tree = new Tree(string.Empty);
			var treeItems = rowHeaders().ToArray();
			var maxColumnNameLength = treeItems.Max(c => c.Length)+2;

			foreach (var row in collection)
			{
				var columns = rowData(row);
				if (columns.Length == 0) continue;

				var columnValue = columns[0];
				var color = colorPicker?.Invoke(0, row);
				var renderer = getRendererForTree(treeItems[0], maxColumnNameLength+4, columnValue, Color.SandyBrown, color);

				var node = tree.AddNode(renderer);

				for (int i = 1; i < treeItems.Length && i < columns.Length; i++)
				{
					var columnName = treeItems[i];
					columnValue = columns[i];
					color = colorPicker?.Invoke(i, row);

					if (!string.IsNullOrWhiteSpace(columnValue))
					{
						renderer = getRendererForTree(columnName, maxColumnNameLength, columnValue, color: color);
						node.AddNode(renderer);
					}
				}
			}

			ansiConsole.Write(tree);
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


	class RowLengthCalculator
	{

		public void NewRow()
		{
			RowLength = 2; // `| `
		}

		public void AddColumn(string column)
		{
			RowLength += column.GetCellWidth() + 3; // ` | `
		}

		public void EndRow()
		{
			RowLength -= 1; // remove the last ` `

			if (RowLength > MaxRowLength)
			{
				MaxRowLength = RowLength;
			}
		}

		public int RowLength { get; private set; } = 0;

		public int MaxRowLength { get; private set; } = 0;
	}
}
