using System.Text;

namespace Greg.Xrm.Command.Services.Output
{
	public class OutputToMemory : IOutput
	{
		private readonly StringBuilder sb = new();

		public IOutput Write(object? text)
		{
			sb.Append(text);
			return this;
		}

		public IOutput Write(object? text, ConsoleColor color)
		{
			sb.Append('<').Append(color.ToString()).Append('>');
			sb.Append(text);
			sb.Append("</").Append(color.ToString()).Append('>');
			return this;
		}

		public IOutput WriteLine(object? text)
		{
			sb.AppendLine(text?.ToString());
			return this;
		}

		public IOutput WriteLine()
		{
			sb.AppendLine();
			return this;
		}

		public IOutput WriteLine(object? text, ConsoleColor color)
		{
			sb.Append('<').Append(color.ToString()).Append('>');
			sb.Append(text);
			sb.Append("</").Append(color.ToString()).Append('>');
			sb.AppendLine();
			return this;
		}

		public override string ToString()
		{
			return sb.ToString();
		}



		public IOutput WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker = null)
		{
			var headers = rowHeaders();
			var rows = collection.Select(rowData).ToList();

			var columnWidths = new int[headers.Length];
			for (var i = 0; i < headers.Length; i++)
			{
				columnWidths[i] = Math.Max(headers[i].Length, rows.Max(_ => _[i].Length));
			}

			var header = "| " + string.Join(" | ", headers.Select((_, i) => _.PadRight(columnWidths[i]))) + " |";
			sb.AppendLine(header);

			var separator = "|-" + string.Join("-|-", columnWidths.Select(_ => new string('-', _))) + "-|";
			sb.AppendLine(separator);

			foreach (var row in rows)
			{
				sb.Append("| ");

				for (var i = 0; i < row.Length; i++)
				{
					sb.Append(row[i].PadRight(columnWidths[i]));
					sb.Append(" | ");
				}
				sb.AppendLine();
			}
			sb.AppendLine();

			return this;
		}
	}
}
